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

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IInventory : IEntityModule
    {
        IReadOnlyList<IMyTerminalBlock> InventoryOwners { get; }
        IMyInventory ToolCargo { get; }
        Dictionary<MyItemType, float> GetAggregateItems();
        Dictionary<MyItemType, float> GetAggregateItemsFor(IList<IMyTerminalBlock> Blocks, Func<MyItemType, bool> filter = null);
        Dictionary<MyItemType, float> GetAggregateItemsFor(IList<IMyInventory> Inventories, Func<MyItemType, bool> filter = null);
        float TryTransferTo(IMyInventory TargetInventory, MyItemType ItemDef, float TargetAmount, IList<IMyInventory> SourceInventories);
        float TryTransferTo(IMyInventory TargetInventory, MyItemType ItemDef, float TargetAmount, IList<IMyTerminalBlock> SourceBlocks);
    }

    public class InventoryModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, UpdatableModule, ClosableModule, IInventory
    {
        public InventoryModule(ILaserToolKernel MyKernel) : base(MyKernel){}

        public override string DebugModuleName { get; } = nameof(InventoryModule);

        public bool Inited { get; private set; }
        public bool RequiresOperable { get; } = false;
        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;
        private int Ticker => MyKernel.Session.Clock.Ticker;
        public IReadOnlyList<IMyTerminalBlock> InventoryOwners => _InventoryOwners.AsReadOnly();
        public IMyInventory ToolCargo { get; private set; }
        private List<IMyTerminalBlock> _InventoryOwners = new List<IMyTerminalBlock>();
        private IMyGridTerminalSystem Term;

        void InitializableModule.Init()
        {
            Inited = true;
            Term = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(MyKernel.Tool.CubeGrid);
            // Maching indexes, oh wow
            ToolCargo = MyKernel.Tool.GetInventory(0);
            MyKernel.Block.AppendingCustomInfo += Block_AppendingCustomInfo;
            RefreshInventoryOwners();
        }

        private void Block_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder info)
        {
            info.AppendLine($"Support inventories: {InventoryOwners.Count}");
        }

        void UpdatableModule.Update()
        {
            if (Ticker % (3 * 60) == 0)
            {
                RefreshInventoryOwners();
            }
        }

        void RefreshInventoryOwners()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            Term.GetBlocksOfType(blocks, IsValidInventory);
            _InventoryOwners.Clear();
            _InventoryOwners.AddRange(blocks);
        }

        bool IsValidInventory(IMyTerminalBlock Block)
        {
            if (!Block.HasInventory) return false;
            bool ValidType = Block is IMyCargoContainer || Block is IMyProductionBlock || Block is IMyShipConnector || Block is IMyCollector || Block is IMyCockpit;
            if (!ValidType) return false;
            bool Connected = Block.GetInventory(0)?.IsConnectedTo(ToolCargo) == true;
            return Connected;
        }

        public Dictionary<MyItemType, float> GetAggregateItems()
        {
            return GetAggregateItemsFor(_InventoryOwners);
        }

        public Dictionary<MyItemType, float> GetAggregateItemsFor(IList<IMyInventory> Inventories, Func<MyItemType, bool> filter = null)
        {
            Dictionary<MyItemType, float> AggregateItems = new Dictionary<MyItemType, float>();

            foreach (IMyInventory Inventory in Inventories)
            {
                if (Inventory == null) continue;
                List<MyInventoryItem> Items = new List<MyInventoryItem>();
                Inventory.GetItems(Items);
                foreach (MyInventoryItem Item in Items)
                {
                    MyItemType itemId = Item.Type;
                    if (filter == null || filter(itemId))
                    {
                        if (!AggregateItems.ContainsKey(itemId))
                            AggregateItems.Add(itemId, (float)Item.Amount);
                        else
                            AggregateItems[itemId] += (float)Item.Amount;
                    }
                }
            }

            return AggregateItems;
        }

        public Dictionary<MyItemType, float> GetAggregateItemsFor(IList<IMyTerminalBlock> Blocks, Func<MyItemType, bool> filter = null)
        {
            return GetAggregateItemsFor(GetInventoriesOutOfBlocks(Blocks), filter);
        }

        public static List<IMyInventory> GetInventoriesOutOfBlocks(IList<IMyTerminalBlock> Blocks)
        {
            List<IMyInventory> Inventories = new List<IMyInventory>();

            foreach (IMyTerminalBlock block in Blocks)
            {
                if (block == null) continue;
                if (!block.HasInventory) continue;
                for (int invIndex = 0; invIndex < block.InventoryCount; invIndex++)
                {
                    IMyInventory Inventory = block.GetInventory(invIndex);
                    Inventories.Add(Inventory);
                }
            }
            return Inventories;
        }

        public float TryTransferTo(IMyInventory TargetInventory, MyItemType ItemDef, float TargetAmount, IList<IMyTerminalBlock> SourceBlocks)
        {
            return TryTransferTo(TargetInventory, ItemDef, TargetAmount, GetInventoriesOutOfBlocks(SourceBlocks));
        }

        public float TryTransferTo(IMyInventory TargetInventory, MyItemType ItemDef, float TargetAmount, IList<IMyInventory> SourceInventories)
        {
            if (TargetInventory == null) throw new ArgumentNullException(nameof(TargetInventory));
            if (SourceInventories == null) throw new ArgumentNullException(nameof(SourceInventories));
            if (SourceInventories.Count == 0) return 0;

            float YetToTransfer = TargetAmount;
            foreach (IMyInventory SourceInventory in SourceInventories)
            {
                List<MyInventoryItem> Items = new List<MyInventoryItem>();
                SourceInventory.GetItems(Items);
                foreach (MyInventoryItem Item in Items)
                {
                    MyItemType def = Item.Type;
                    float amount = (float)Item.Amount;

                    if (Item.Type != ItemDef) continue;
                    float pullAmount = Math.Min(amount, YetToTransfer);
                    if (TargetInventory.CanItemsBeAdded((VRage.MyFixedPoint)pullAmount, Item.Type))
                    {
                        bool pull = SourceInventory.TransferItemTo(TargetInventory, Items.IndexOf(Item), amount: (VRage.MyFixedPoint)pullAmount);
                        if (pull)
                        {
                            YetToTransfer -= pullAmount;
                            if (YetToTransfer <= 0) return TargetAmount;
                        }
                    }
                    else
                    {
                        float FittableAmount = (float)(TargetInventory as Sandbox.Game.MyInventory).ComputeAmountThatFits(Item.Type);
                        if (FittableAmount > pullAmount) FittableAmount = pullAmount;
                        bool pull = SourceInventory.TransferItemTo(TargetInventory, Items.IndexOf(Item), amount: (VRage.MyFixedPoint)FittableAmount);
                        if (pull)
                        {
                            YetToTransfer -= (float)FittableAmount;
                            return TargetAmount - YetToTransfer;
                        }
                    }
                }
            }
            return TargetAmount - YetToTransfer;
        }

        public static Dictionary<MyItemType, float> CalculateMissingItems(Dictionary<MyItemType, float> AggregateInputItems, Dictionary<MyItemType, float> AggregateItems)
        {
            Dictionary<MyItemType, float> MissingItems = new Dictionary<MyItemType, float>();
            foreach (KeyValuePair<MyItemType, float> InputItem in AggregateInputItems)
            {
                if (!AggregateItems.ContainsKey(InputItem.Key))
                    MissingItems.Add(InputItem.Key, InputItem.Value);
                else
                {
                    float remainingAmount = InputItem.Value - AggregateItems[InputItem.Key];
                    if (remainingAmount > 0)
                        MissingItems.Add(InputItem.Key, remainingAmount);
                }
            }
            return MissingItems;
        }

        void ClosableModule.Close()
        {
            MyKernel.Block.AppendingCustomInfo -= Block_AppendingCustomInfo;
        }
    }
}
