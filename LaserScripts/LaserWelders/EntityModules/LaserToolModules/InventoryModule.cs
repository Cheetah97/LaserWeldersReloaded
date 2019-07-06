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
using EemRdx.Extensions;

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
        float TryTransferTo(IList<IMyInventory> TargetInventories, MyItemType ItemDef, float TargetAmount, IList<IMyInventory> SourceInventories);
        float TryTransferTo(IList<IMyInventory> TargetInventories, MyItemType ItemDef, float TargetAmount, IList<IMyTerminalBlock> SourceBlocks);
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
        private List<IMyTerminalBlock> _InventoryOwners = new List<IMyTerminalBlock>(40);
        private IMyGridTerminalSystem Term;
        private GridModules.IInventorySystem InventorySystem;
        private bool HasSubscribed = false;

        void InitializableModule.Init()
        {
            GridKernel gridKernel;
            if (MyKernel.Block.CubeGrid.TryGetComponent(out gridKernel))
            {
                Inited = true;
                Term = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(MyKernel.Tool.CubeGrid);
                InventorySystem = gridKernel.InventorySystem;
                // Maching indexes, oh wow
                ToolCargo = MyKernel.Tool.GetInventory(0);
                MyKernel.Block.AppendingCustomInfo += Block_AppendingCustomInfo;
                //RefreshInventoryOwners();
            }
        }

        private void Block_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder info)
        {
            info.AppendLine($"Support inventories: {InventoryOwners.Count}");
        }

        void UpdatableModule.Update()
        {
            if (Ticker == 3) UpdateSubscription();
            if (Ticker > 0 && Ticker % 60 == 0)
            {
                UpdateSubscription();
                RefreshInventoryOwners();
            }
        }

        private void UpdateSubscription()
        {
            bool shouldBeSubscribed = MyKernel.Tool.UseConveyorSystem && MyKernel.OperabilityProvider.CanOperate && !MyKernel.ConcealmentDetectionModule.IsLikelyConcealed;
            if (shouldBeSubscribed && !HasSubscribed)
            {
                InventorySystem.Subscribe(MyKernel);
                HasSubscribed = true;
            }
            else if (!shouldBeSubscribed && HasSubscribed)
            {
                InventorySystem.Unsubscribe(MyKernel);
                HasSubscribed = false;
            }
        }

        void RefreshInventoryOwners()
        {
            _InventoryOwners.Clear();
            if (MyKernel.Tool.UseConveyorSystem && !MyKernel.ConcealmentDetectionModule.IsLikelyConcealed)
            {
                List<IMyTerminalBlock> inventoryOwners = new List<IMyTerminalBlock>(_InventoryOwners.Capacity);
                InventorySystem.GetInventoriesForTool(MyKernel, ref inventoryOwners);

                _InventoryOwners.AddRange(inventoryOwners.OrderByDescending(GetInventoryOrder));
                inventoryOwners.Clear();
                inventoryOwners = null;
            }
        }

        private static int GetInventoryOrder(IMyTerminalBlock Block)
        {
            if (Block is IMyCargoContainer) return 100;
            if (Block is IMyProductionBlock) return 50;
            if (Block is IMyShipConnector) return 40;
            if (Block is IMyCollector) return 30;
            if (Block is IMyCockpit) return 20;
            return 0;
        }

        bool IsValidInventory(IMyTerminalBlock Block)
        {
            bool ValidType = Block is IMyCargoContainer || Block is IMyProductionBlock || Block is IMyShipConnector || Block is IMyCollector || Block is IMyCockpit;
            return ValidType;
        }

        public Dictionary<MyItemType, float> GetAggregateItems()
        {
            return GetAggregateItemsFor(_InventoryOwners);
        }

        public Dictionary<MyItemType, float> GetAggregateItemsFor(IList<IMyInventory> Inventories, Func<MyItemType, bool> filter = null)
        {
            Dictionary<MyItemType, float> AggregateItems = new Dictionary<MyItemType, float>(40);

            foreach (IMyInventory Inventory in Inventories)
            {
                if (Inventory == null) continue;
                List<MyInventoryItem> Items = new List<MyInventoryItem>(40);
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
            List<IMyInventory> Inventories = new List<IMyInventory>(40);

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
            List<MyInventoryItem> Items = new List<MyInventoryItem>(40);
            foreach (IMyInventory SourceInventory in SourceInventories)
            {
                if (!SourceInventory.Owner.HasInventory) continue;
                if (SourceInventory.Owner is IMyTerminalBlock)
                {
                    IMyTerminalBlock SourceBlock = SourceInventory.Owner as IMyTerminalBlock;
                    if (!SourceBlock.IsFunctional) continue;
                    if (!SourceBlock.HasPlayerAccess(MyKernel.Tool.OwnerId)) continue;
                }

                Items.Clear();
                if (Items.Capacity < 40) Items.Capacity = 40;
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

        public float TryTransferTo(IList<IMyInventory> TargetInventories, MyItemType ItemDef, float TargetAmount, IList<IMyTerminalBlock> SourceBlocks)
        {
            return TryTransferTo(TargetInventories, ItemDef, TargetAmount, GetInventoriesOutOfBlocks(SourceBlocks));
        }

        public float TryTransferTo(IList<IMyInventory> TargetInventories, MyItemType ItemDef, float TargetAmount, IList<IMyInventory> SourceInventories)
        {
            if (TargetInventories == null) throw new ArgumentNullException(nameof(TargetInventories));
            if (SourceInventories == null) throw new ArgumentNullException(nameof(SourceInventories));
            if (TargetInventories.Count == 0) return 0;
            if (SourceInventories.Count == 0) return 0;

            float YetToTransfer = TargetAmount;
            foreach (IMyInventory TargetInventory in TargetInventories)
            {
                YetToTransfer -= TryTransferTo(TargetInventory, ItemDef, TargetAmount, SourceInventories);
                if (YetToTransfer <= 0) return TargetAmount;
            }
            return TargetAmount - YetToTransfer;
        }


        public static Dictionary<MyItemType, float> CalculateMissingItems(Dictionary<MyItemType, float> AggregateInputItems, Dictionary<MyItemType, float> AggregateItems)
        {
            Dictionary<MyItemType, float> MissingItems = new Dictionary<MyItemType, float>(40);
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
            if (InventorySystem != null) InventorySystem.Unsubscribe(MyKernel);
        }
    }
}
