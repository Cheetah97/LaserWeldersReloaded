using Sandbox.Game;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.LaserWelders.Helpers
{
    public static class InventoryHelpers
    {
        public static List<IMyInventory> GetInventories(this IMyEntity Entity)
        {
            List<IMyInventory> Inventories = new List<IMyInventory>();

            for (int i = 0; i < Entity.InventoryCount; ++i)
            {
                var blockInventory = Entity.GetInventory(i) as MyInventory;
                if (blockInventory != null) Inventories.Add(blockInventory);
            }

            return Inventories;
        }

        public static void PickupItem(this IMyInventory Inventory, IMyFloatingObject FloatingObject)
        {
            (Inventory as MyInventory).TakeFloatingObject(FloatingObject as MyFloatingObject);
        }

        public static VRage.MyFixedPoint GetAmount(this IMyFloatingObject FloatingObject)
        {
            return (FloatingObject as MyFloatingObject).Amount;
        }

        public static void ReadMissingComponentsSmart(this IMySlimBlock Block, Dictionary<string, int> addToDictionary)
        {
            if (Block.BuildIntegrity == Block.MaxIntegrity && Block.Integrity == Block.MaxIntegrity) return;
            Block.GetMissingComponents(addToDictionary);
            if (Block.StockpileAllocated)
            {
                Block.GetMissingComponents(addToDictionary);
            }
            else
            {
                foreach (var Component in (Block.BlockDefinition as Sandbox.Definitions.MyCubeBlockDefinition).Components)
                {
                    string Name = Component.Definition.Id.SubtypeName;
                    if (addToDictionary.ContainsKey(Name)) addToDictionary[Name] += Component.Count;
                    else addToDictionary.Add(Name, Component.Count);
                }
            }
        }
    }
}
