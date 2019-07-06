using EemRdx.EntityModules;
using EemRdx.LaserWelders.Helpers;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using MyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace EemRdx.LaserWelders.EntityModules.GridModules
{
    public interface IInventorySystem : IEntityModule
    {
        void Subscribe(ILaserToolKernel laserTool);
        void Unsubscribe(ILaserToolKernel laserTool);
        void GetInventoriesForTool(ILaserToolKernel LaserTool, ref List<IMyTerminalBlock> SupportInventories);

        IReadOnlyList<IMyTerminalBlock> InventoryOwners { get; }
    }

    public class InventorySystemModule : EntityModuleBase<IGridKernel>, InitializableModule, UpdatableModule, IInventorySystem
    {
        public InventorySystemModule(IGridKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(InventorySystemModule);

        public bool Inited { get; private set; }
        public bool RequiresOperable { get; } = false;
        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;
        private int Ticker => MyKernel.Session.Clock.Ticker;
        public IReadOnlyList<IMyTerminalBlock> InventoryOwners => _InventoryOwners.AsReadOnly();
        private List<IMyTerminalBlock> _InventoryOwners = new List<IMyTerminalBlock>(40);
        private Dictionary<long, ILaserToolKernel> SubscribedLaserTools = new Dictionary<long, ILaserToolKernel>(20);
        private Dictionary<ILaserToolKernel, HashSet<IMyTerminalBlock>> InventoriesPerTool = new Dictionary<ILaserToolKernel, HashSet<IMyTerminalBlock>>();
        private IMyGridTerminalSystem Term;
        private int OpLimit => MyKernel.Session.Settings.MaxInventoryUpdatePerTick;
        private readonly int updateSkipTicks = 3 * 60;

        void InitializableModule.Init()
        {
            Inited = true;
            Term = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(MyKernel.Grid);
        }

        public void Subscribe(ILaserToolKernel laserTool)
        {
            SubscribedLaserTools.Add(laserTool.Block.EntityId, laserTool);
        }

        public void Unsubscribe(ILaserToolKernel laserTool)
        {
            SubscribedLaserTools.Remove(laserTool.Block.EntityId);
        }

        public void GetInventoriesForTool(ILaserToolKernel LaserTool, ref List<IMyTerminalBlock> SupportInventories)
        {
            if (MyKernel.Multigridder?.BiggestGrid?.InventorySystem == null) return;
            InventorySystemModule BiggestGridInventorySystem = (MyKernel.Multigridder.BiggestGrid.InventorySystem as InventorySystemModule);
            if (BiggestGridInventorySystem.InventoriesPerTool.ContainsKey(LaserTool)) SupportInventories.AddRange(BiggestGridInventorySystem.InventoriesPerTool[LaserTool]);
            else
            {
                if (MyKernel.Session.Settings.Debug)
                    WriteToLog(nameof(GetInventoriesForTool), $"No entries were found for tool {LaserTool.Block.CustomName} on grid {LaserTool.Block.CubeGrid.EntityId}");
            }
        }

        //public void GetInventoriesForTool(ILaserToolKernel LaserTool, ref List<IMyTerminalBlock> SupportInventories)
        //{
        //    if (InventoriesPerTool.ContainsKey(LaserTool)) SupportInventories.AddRange(InventoriesPerTool[LaserTool]);
        //}

        IEnumerator<bool> Worker = null;
        int DueStartTick = 5;
        private void RunUpdate()
        {
            if (Worker != null)
            {
                Worker.MoveNext();
                bool working = Worker.Current;
                if (!working)
                {
                    Worker.Dispose();
                    Worker = null;
                }
            }
        }

        void UpdatableModule.Update()
        {
            if (Worker == null && Ticker >= DueStartTick && MyKernel.Multigridder.BiggestGrid.Grid.EntityId == MyKernel.Grid.EntityId)
            {
                if (Ticker > 10) Worker = RefreshInventoryOwners();
                else Worker = RefreshInventoryOwners(BypassLoadChecks: true);
            }

            if (Worker != null) RunUpdate();
        }

        private Dictionary<long, ILaserToolKernel> CombineSubscribed()
        {
            Dictionary<long, ILaserToolKernel> subscribedLaserTools = new Dictionary<long, ILaserToolKernel>();

            foreach (IGridKernel Kernel in MyKernel.Multigridder.CompleteGrid)
            {
                foreach (KeyValuePair<long, ILaserToolKernel> kvp in (Kernel.InventorySystem as InventorySystemModule).SubscribedLaserTools)
                {
                    if (!subscribedLaserTools.ContainsKey(kvp.Key))
                        subscribedLaserTools.Add(kvp.Key, kvp.Value);
                }
            }

            return subscribedLaserTools;
        }

        IEnumerator<bool> RefreshInventoryOwners(bool BypassLoadChecks = false)
        {
            bool useDebug = MyKernel.Session.Settings.Debug;
            int opStart = Ticker;

            Dictionary<long, ILaserToolKernel> subscribedLaserTools = CombineSubscribed();
            Dictionary<ILaserToolKernel, HashSet<IMyTerminalBlock>> inventoriesPerTool = new Dictionary<ILaserToolKernel, HashSet<IMyTerminalBlock>>();

            if (SubscribedLaserTools.Count == 0) yield return false;

            List<IMyTerminalBlock> inventoryOwners = new List<IMyTerminalBlock>(80);
            Term.GetBlocksOfType(inventoryOwners, IsValidInventory);

            if (useDebug)
            {
                WriteToLog(nameof(RefreshInventoryOwners), $"Combined subscribed tools: {subscribedLaserTools.Count}");
                WriteToLog(nameof(RefreshInventoryOwners), $"Combined valid inventories: {inventoryOwners.Count}");
            }

            int capacity = subscribedLaserTools.Count + 1;
            Dictionary<long, HashSet<long>> ToolsInterlinks = new Dictionary<long, HashSet<long>>(capacity);
            Dictionary<long, HashSet<long>> ToolsFailedInterlinks = new Dictionary<long, HashSet<long>>(capacity);

            int opCounter1 = 0;
            foreach (ILaserToolKernel LaserTool in subscribedLaserTools.Values)
            {
                long ToolId = LaserTool.Block.EntityId;
                if (!ToolsInterlinks.ContainsKey(ToolId)) ToolsInterlinks.Add(ToolId, new HashSet<long>());
                if (!ToolsFailedInterlinks.ContainsKey(ToolId)) ToolsFailedInterlinks.Add(ToolId, new HashSet<long>());
                if (!inventoriesPerTool.ContainsKey(LaserTool)) inventoriesPerTool.Add(LaserTool, new HashSet<IMyTerminalBlock>());

                HashSet<long> ToolInterlinks = ToolsInterlinks[ToolId];
                HashSet<long> ToolFailedInterlinks = ToolsFailedInterlinks[ToolId];
                IMyInventory ToolCargo = LaserTool.Inventory.ToolCargo;

                foreach (ILaserToolKernel OtherLaserTool in subscribedLaserTools.Values)
                {
                    long OtherToolId = OtherLaserTool.Block.EntityId;
                    if (ToolId == OtherToolId) continue;

                    if (!ToolsInterlinks.ContainsKey(OtherToolId)) ToolsInterlinks.Add(OtherToolId, new HashSet<long>());
                    if (!ToolsFailedInterlinks.ContainsKey(OtherToolId)) ToolsFailedInterlinks.Add(OtherToolId, new HashSet<long>());
                    if (!inventoriesPerTool.ContainsKey(OtherLaserTool)) inventoriesPerTool.Add(OtherLaserTool, new HashSet<IMyTerminalBlock>());

                    IMyInventory OtherToolCargo = OtherLaserTool.Inventory.ToolCargo;
                    HashSet<long> OtherToolInterlinks = ToolsInterlinks[OtherToolId];
                    HashSet<long> OtherToolFailedInterlinks = ToolsFailedInterlinks[OtherToolId];

                    if (!ToolInterlinks.Contains(OtherToolId) && !ToolFailedInterlinks.Contains(OtherToolId))
                    {
                        bool Accessible = OtherLaserTool.Block.HasPlayerAccess(LaserTool.Block.OwnerId);
                        bool Connected = Accessible && ToolCargo.IsConnectedTo(OtherToolCargo);

                        if (Connected)
                        {
                            ToolInterlinks.Add(OtherToolId);
                            OtherToolInterlinks.Add(ToolId);
                        }
                        else
                        {
                            ToolFailedInterlinks.Add(OtherToolId);
                            OtherToolFailedInterlinks.Add(ToolId);
                        }
                        opCounter1++;
                    }

                    if (opCounter1 % OpLimit == 0 && !BypassLoadChecks) yield return true;
                }
            }
            if (!BypassLoadChecks) yield return true;
            opCounter1 = 0;
            foreach (IMyTerminalBlock InventoryOwner in inventoryOwners)
            {
                IMyInventory Inventory = InventoryOwner.GetInventory(0);
                foreach (ILaserToolKernel LaserTool in subscribedLaserTools.Values)
                {
                    HashSet<IMyTerminalBlock> ToolSupportInventories = inventoriesPerTool[LaserTool];
                    if (ToolSupportInventories.Contains(InventoryOwner)) continue;
                    long ToolId = LaserTool.Block.EntityId;
                    IMyInventory ToolCargo = LaserTool.Inventory.ToolCargo;
                    HashSet<long> ToolInterlinks = ToolsInterlinks[ToolId];

                    bool Connected = Inventory.IsConnectedTo(ToolCargo);
                    if (Connected)
                    {
                        inventoriesPerTool[LaserTool].Add(InventoryOwner);

                        foreach (ILaserToolKernel OtherLaserTool in subscribedLaserTools.Values)
                        {
                            if (ToolId == OtherLaserTool.Block.EntityId) continue;
                            inventoriesPerTool[OtherLaserTool].Add(InventoryOwner);
                        }
                        opCounter1++;
                    }
                    if (opCounter1 % OpLimit == 0 && !BypassLoadChecks) yield return true;
                }
            }

            _InventoryOwners.Clear();
            InventoriesPerTool.Clear();

            if (_InventoryOwners.Capacity < inventoryOwners.Count + 10) _InventoryOwners.Capacity = inventoryOwners.Count + 10;
            _InventoryOwners.AddRange(inventoryOwners);

            foreach (KeyValuePair<ILaserToolKernel, HashSet<IMyTerminalBlock>> kvp in inventoriesPerTool)
            {
                InventoriesPerTool.Add(kvp.Key, kvp.Value);
            }

            int updateTook = Ticker - opStart;
            if (useDebug)
            {
                WriteToLog(nameof(RefreshInventoryOwners), $"Tool/inventories pairs: {InventoriesPerTool.Count}");

                WriteToLog(nameof(RefreshInventoryOwners), $"Tool/inventories working pairs: {InventoriesPerTool.Count(x => x.Value.Count > 0)}");

                WriteToLog(nameof(RefreshInventoryOwners), $"Update took: {updateTook} ticks");
            }

            DueStartTick = Ticker + updateSkipTicks + (updateTook * 2);
            yield return false;
        }

        bool IsValidInventory(IMyTerminalBlock Block)
        {
            bool ValidType = Block is IMyCargoContainer || Block is IMyProductionBlock || Block is IMyShipConnector || Block is IMyCollector || Block is IMyCockpit;
            return ValidType;
        }
    }
}
