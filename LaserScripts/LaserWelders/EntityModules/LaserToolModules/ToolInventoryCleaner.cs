using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using MyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;
using EemRdx.EntityModules;
using System;
using Sandbox.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public class ToolInventoryCleaner : EntityModuleBase<ILaserToolKernel>, InitializableModule, UpdatableModule
    {
        public ToolInventoryCleaner(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(ToolInventoryCleaner);
        public bool Inited { get; private set; }
        bool UpdatableModule.RequiresOperable { get; } = false;
        MyEntityUpdateEnum UpdatableModule.UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;

        IInventory Inventory => MyKernel.Inventory;
        IMyInventory ToolCargo => Inventory.ToolCargo;

        void InitializableModule.Init()
        {
            if (Inited) return;

            Inited = true;
        }

        void UpdatableModule.Update()
        {
            if (MyKernel.Session.Clock.Ticker % 60 == 0) CleanInventory();
        }

        private bool WasToggledLastSecond;
        private void CleanInventory()
        {
            bool CanWork = MyKernel.Block.IsFunctional && MyKernel.PowerModule?.SufficientPower == true;
            if (!CanWork) return;

            bool Toggled = MyKernel.Toggle.Toggled;
            float fillPercentage = (float)ToolCargo.CurrentVolume / (float)ToolCargo.MaxVolume;

            bool CleanOnTooFull = fillPercentage > 0.8f;
            bool CleanOnUnused = fillPercentage > 0 && !Toggled && !WasToggledLastSecond;

            if (CleanOnTooFull || CleanOnUnused)
                PushInventory();

            WasToggledLastSecond = Toggled;
        }

        private void PushInventory()
        {
            List<MyInventoryItem> AllItems = new List<MyInventoryItem>();
            ToolCargo.GetItems(AllItems);
            List<IMyInventory> PushInventories = GetPushInventories();
            List<IMyTerminalBlock> tempList = new List<IMyTerminalBlock> { MyKernel.Block };
            foreach (MyInventoryItem Item in AllItems.OrderByDescending(x => (float)x.Amount))
            {
                Inventory.TryTransferTo(PushInventories, Item.Type, (float)Item.Amount, tempList);
            }
        }

        private List<IMyInventory> GetPushInventories()
        {
            Func<IMyTerminalBlock, int> GetPushPriority = Block =>
            {
                if (Block is IMyCargoContainer) return 50;
                if (Block is IMyShipConnector) return 40;
                if (Block is IMyCollector) return 30;

                return 0;
            };

            List<IMyInventory> Inventories = new List<IMyInventory>();
            foreach (IMyTerminalBlock Block in Inventory.InventoryOwners.OrderByDescending(GetPushPriority))
            {
                IMyInventory inventory = Block.GetInventory(0);
                Inventories.Add(inventory);
            }

            return Inventories;
        }
    }
}
