using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace EemRdx.Extensions
{
	public static class TerminalExtensions
	{
		public static IMyGridTerminalSystem GetTerminalSystem(this IMyCubeGrid grid)
		{
			return MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
		}

		/// <summary>
		/// Allows GetBlocksOfType to work like a chainable function.
		/// <para />
		/// Enjoy allocating.
		/// </summary>
		public static List<T> GetBlocksOfType<T>(this IMyGridTerminalSystem Term, Func<T, bool> collect = null) where T : class, Sandbox.ModAPI.Ingame.IMyTerminalBlock
		{
			List<T> termBlocks = new List<T>();
			Term.GetBlocksOfType<T>(termBlocks, collect);
			return termBlocks;
		}

		//public static void Trigger(this IMyTimerBlock Timer)
		//{
		//	Timer.GetActionWithName("TriggerNow").Apply(Timer);
		//}

		//public static List<IMyInventory> GetInventories(this IMyEntity Entity)
		//{
		//	if (!Entity.HasInventory) return new List<IMyInventory>();

		//	List<IMyInventory> Inventories = new List<IMyInventory>();
		//	for (int i=0; i<Entity.InventoryCount; i++)
		//	{
		//		Inventories.Add(Entity.GetInventory(i));
		//	}
		//	return Inventories;
		//}
	}
}