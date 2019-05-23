using System.Collections.Generic;
using System.Linq;
using EemRdx.Extensions;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace EemRdx.Helpers
{
	public static class OwnershipTools
	{
		public static long PirateId => MyVisualScriptLogicProvider.GetPirateId();

		public static bool IsOwnedByPirates(this IMyTerminalBlock block)
		{
			return block.OwnerId == PirateId;
		}

        // TODO: Check in which cases the player can be null
		public static bool IsOwnedByNpc(this IMyTerminalBlock block, bool allowNobody = true, bool checkBuilder = false)
		{
			if (!checkBuilder)
			{
				if (block.IsOwnedByPirates()) return true;
				if (!allowNobody && block.IsOwnedByNobody()) return false;
				IMyPlayer owner = MyAPIGateway.Players.GetPlayerById(block.OwnerId);
                return owner?.IsBot == true;
            }
			else
			{
				if (!block.IsOwnedByNpc(allowNobody)) return false;
				long builderId = block.GetBuiltBy();
				if (!allowNobody && builderId == 0) return false;
				IMyPlayer owner = MyAPIGateway.Players.GetPlayerById(builderId);
                return owner?.IsBot == true;
            }
		}

        public static bool IsOwnedByNpcFaction(this IMyTerminalBlock Block)
        {
            string OwnerFactionTag = Block.GetOwnerFactionTag();
            //if (string.IsNullOrEmpty(OwnerFactionTag)) return false;
            //return Constants.NpcFactions.Contains(OwnerFactionTag);
            return OwnerFactionTag == "SPRT";
        }

		public static bool IsPirate(this IMyCubeGrid grid, bool strictCheck = false)
		{
			if (grid.BigOwners.Count == 0 || grid.BigOwners[0] == 0) return false;
			if (!strictCheck) return grid.BigOwners.Contains(PirateId);
			return grid.BigOwners.Count == 1 && grid.BigOwners[0] == PirateId;
		}

		public static bool IsNpc(this IMyCubeGrid grid)
		{
			if (grid.IsPirate()) return true;
			if (grid.BigOwners.Count == 0) return false;
			IMyPlayer owner = MyAPIGateway.Players.GetPlayerById(grid.BigOwners[0]);
			return owner == null || owner.IsBot;
		}

		public static bool IsOwnedByNobody(this IMyCubeGrid grid)
		{
			return grid.BigOwners.Count == 0 || grid.BigOwners[0] == 0;
		}

		public static bool IsOwnedByNobody(this IMyCubeBlock block)
		{
			return block.OwnerId == 0;
		}

		public static bool IsBuiltByNobody(this IMyCubeBlock block)
		{
			return block.GetBuiltBy() == 0;
		}

		public static bool IsPlayerBlock(this IMySlimBlock block, out IMyPlayer builder)
		{
			builder = null;
			long builtBy = block.GetBuiltBy();
			if (builtBy == 0) return false;
			builder = MyAPIGateway.Players.GetPlayerById(builtBy);
			return builder != null && !builder.IsBot;
		}

		public static bool IsPlayerBlock(this IMyCubeBlock block, out IMyPlayer owner)
		{
			owner = null;
			if (block.OwnerId != 0)
			{
				return MyAPIGateway.Players.IsValidPlayer(block.OwnerId, out owner);
			}
			else
			{
                return MyAPIGateway.Players.IsValidPlayer(block.GetBuiltBy(), out owner);
			}
		}

        /// <summary>
        /// Groups a list of blocks by owner. Owner is determined by terminal owner first and BuiltBy if first fails.
        /// <para/>
        /// May and will add a group with Key == null if ownership check fails completely.
        /// </summary>
        public static List<IGrouping<IMyPlayer, IMyTerminalBlock>> AcquireBlocksOwners(IEnumerable<IMyTerminalBlock> Blocks)
        {
            var Output = Blocks.GroupBy(x => x.OwnerId != 0 ? MyAPIGateway.Players.GetPlayerById(x.OwnerId) : MyAPIGateway.Players.GetPlayerById(x.GetBuiltBy())).ToList();
            return Output;
        }
    }
}