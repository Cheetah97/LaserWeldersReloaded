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
        private readonly int updateSkipTicks = (3 * 60);
        private bool useDebug => MyKernel.Session.Settings.Debug;

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
                    WriteToLog(nameof(GetInventoriesForTool), $"No entries were found for tool {LaserTool.Block.CustomName} on grid {LaserTool.Block.CubeGrid.CustomName}. Biggest grid's InventoriesPerTool: {BiggestGridInventorySystem.InventoriesPerTool.Count}, biggest grid's Inventory Owners: {BiggestGridInventorySystem.InventoryOwners.Count}");
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
            if (Worker == null) return;

            bool move = false;
            try
            {
                move = Worker.MoveNext();
            }
            catch (Exception Scrap)
            {
                LogError(nameof(RunUpdate), "Worker crashed:\r\n", Scrap);
            }
            
            bool working = Worker.Current;
            if (!(working && move))
            {
                Worker.Dispose();
                Worker = null;
            }
        }

        void UpdatableModule.Update()
        {
            bool isBiggestGrid = MyKernel.Multigridder.BiggestGrid.Grid.EntityId == MyKernel.Grid.EntityId;

            if (Worker == null && Ticker >= DueStartTick)
            {
                DueStartTick = Ticker + updateSkipTicks;
                if (useDebug && MyKernel.Multigridder.CompleteGrid.Count > 1)
                {
                    WriteToLog("Update", $"Multigridding: Choosing {(isBiggestGrid ? "in favor of" : "against")} updating");
                }

                if (isBiggestGrid) Worker = RefreshInventoryOwners(BypassLoadChecks: Ticker < 10);
            }

            if (Worker != null) RunUpdate();
        }

        private Dictionary<long, ILaserToolKernel> CombineSubscribed()
        {
            Dictionary<long, ILaserToolKernel> subscribedLaserTools = new Dictionary<long, ILaserToolKernel>();

            foreach (IGridKernel Kernel in MyKernel.Multigridder.CompleteGrid)
            {
                var KernelTools = (Kernel.InventorySystem as InventorySystemModule).SubscribedLaserTools;
                if (useDebug && MyKernel.Multigridder.CompleteGrid.Count > 1)
                {
                    WriteToLog("CombineSubscribed", $"Subgrid: {Kernel.Grid.CustomName}, subscribed tools: {KernelTools.Count}");
                }
                foreach (KeyValuePair<long, ILaserToolKernel> kvp in KernelTools)
                {
                    if (!subscribedLaserTools.ContainsKey(kvp.Key))
                        subscribedLaserTools.Add(kvp.Key, kvp.Value);
                    else
                    {
                        WriteToLog("CombineSubscribed", $"Subgrid: {Kernel.Grid.CustomName}: Omitting tool {kvp.Value.Block.CustomName}, already in dictionary with key {kvp.Key} (block's id {kvp.Value.Block.EntityId})");
                    }
                }
            }

            if (useDebug && MyKernel.Multigridder.CompleteGrid.Count > 1)
            {
                WriteToLog("CombineSubscribed", $"Complete grid: {MyKernel.Multigridder.CompleteGrid.Count} grids, largest: {MyKernel.Multigridder.BiggestGrid.Grid.CustomName}, subscribed tools: {subscribedLaserTools.Count}");
            }

            return subscribedLaserTools;
        }

        IEnumerator<bool> RefreshInventoryOwners(bool BypassLoadChecks = false)
        {
            int opStart = Ticker;

            bool shouldUseDebug = useDebug && MyKernel.Multigridder.CompleteGrid.Count > 1;

            if (shouldUseDebug)
            {
                WriteToLog(nameof(RefreshInventoryOwners), $"Starting update...");
            }

            Dictionary<long, ILaserToolKernel> subscribedLaserTools = CombineSubscribed();
            Dictionary<ILaserToolKernel, HashSet<IMyTerminalBlock>> inventoriesPerTool = new Dictionary<ILaserToolKernel, HashSet<IMyTerminalBlock>>();

            if (subscribedLaserTools.Count == 0)
            {
                if (shouldUseDebug) WriteToLog(nameof(RefreshInventoryOwners), $"Early exit: no subscribed tools");
                yield return false;
            }

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
            if (shouldUseDebug)
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
