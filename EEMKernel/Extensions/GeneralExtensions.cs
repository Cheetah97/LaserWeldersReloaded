using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.Extensions
{
	public static class GeneralExtensions
	{
		public static bool IsNullEmptyOrWhiteSpace(this string str)
		{
			return string.IsNullOrWhiteSpace(str);
		}

		//public static bool IsValid(this Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entityInfo)
		//{
		//	return !entityInfo.IsEmpty();
		//}

		public static bool IsHostile(this Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entityInfo)
		{
			return entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies;
		}

		public static bool IsNonFriendly(this Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entityInfo)
		{
			return entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies || entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral;
		}

		public static IMyEntity GetEntity(this Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entityInfo)
		{
			return MyAPIGateway.Entities.GetEntityById(entityInfo.EntityId);
		}

		/// <summary>
		/// Retrieves entity mass, in tonnes.
		/// </summary>
		public static float GetMassT(this Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entityInfo)
		{
			return entityInfo.GetEntity().Physics.Mass / 1000;
		}

		public static IMyCubeGrid GetGrid(this Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entityInfo)
		{
			if (!entityInfo.IsGrid()) return null;
			return MyAPIGateway.Entities.GetEntityById(entityInfo.EntityId) as IMyCubeGrid;
		}

		public static bool IsGrid(this Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entityInfo)
		{
			return entityInfo.Type == Sandbox.ModAPI.Ingame.MyDetectedEntityType.SmallGrid || entityInfo.Type == Sandbox.ModAPI.Ingame.MyDetectedEntityType.LargeGrid;
		}

		//public static void EnsureName(this IMyEntity entity, string desiredName = null)
		//{
		//	if (entity == null) return;
		//	if (desiredName == null) desiredName = $"Entity_{entity.EntityId}";
		//	entity.Name = desiredName;
		//	MyAPIGateway.Entities.SetEntityName(entity, false);
		//}

		public static IMyFaction GetFaction(this IMyPlayer player)
		{
            if (player == null)
            {
                if (Helpers.Constants.AllowThrowingErrors) throw new ArgumentNullException("player");
                else return null;
            }

			return MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
		}

		public static bool IsMainCockpit(this IMyShipController shipController)
		{
			return ((MyShipController) shipController).IsMainCockpit;
		}

		/// <summary>
		/// Returns block's builder id.
		/// </summary>
		public static long GetBuiltBy(this IMyCubeBlock block)
		{
			return ((MyCubeBlock) block).BuiltBy;
		}

        public static List<IMyPlayer> GetBuiltBy(this IEnumerable<IMyCubeBlock> Blocks)
        {
            List<IMyPlayer> Result = new List<IMyPlayer>();

            foreach (long BuilderId in Blocks.GroupBy(x => x.GetBuiltBy()).Select(x => x.Key))
            {
                IMyPlayer Builder = MyAPIGateway.Players.GetPlayerById(BuilderId);
                if (Builder != null && !Result.Contains(Builder)) Result.Add(Builder);
            }

            return Result;
        }

        public static List<IMyPlayer> GetOwners(this IEnumerable<IMyCubeBlock> Blocks)
        {
            List<IMyPlayer> Result = new List<IMyPlayer>();

            foreach (long OwnerId in Blocks.GroupBy(x => x.OwnerId).Select(x => x.Key))
            {
                IMyPlayer Owner = MyAPIGateway.Players.GetPlayerById(OwnerId);
                if (Owner != null && !Result.Contains(Owner)) Result.Add(Owner);
            }

            return Result;
        }

        public static List<IMyPlayer> GetGunners(this IEnumerable<IMyLargeTurretBase> Turrets)
        {
            List<IMyPlayer> Result = new List<IMyPlayer>();

            foreach (IMyLargeTurretBase Turret in Turrets)
            {
                IMyPlayer Gunner = MyAPIGateway.Players.GetPlayerControllingEntity(Turret);
                if (Gunner != null && !Result.Contains(Gunner)) Result.Add(Gunner);
            }

            return Result;
        }

        /// <summary>
        /// Returns block's builder id. WARNING: Heavy!
        /// </summary>
        public static long GetBuiltBy(this IMySlimBlock block)
		{
            if (block is IMyCubeBlock)
				return (block as MyCubeBlock).BuiltBy;
			MyObjectBuilder_CubeBlock builder = block.GetObjectBuilder();
			return builder.BuiltBy;
		}

		public static bool IsNpc(this IMyFaction faction)
		{
			try
			{
				IMyPlayer owner = MyAPIGateway.Players.GetPlayerById(faction.FounderId);
				if (owner != null) return owner.IsBot;
				if (!faction.Members.Any()) return true;
				foreach (KeyValuePair<long, MyFactionMember> myMember in faction.Members)
				{
					IMyPlayer member = MyAPIGateway.Players.GetPlayerById(myMember.Value.PlayerId);
					if (member == null) continue;
					if (!member.IsBot) return false;
				}
				return true;
			}
			catch //(Exception scrap)
			{
				//EEMSessionKernel.Static.Log.GeneralLog?.LogError("Faction.IsNPC()", "", scrap);
				return false;
			}
		}

		public static bool IsPlayerFaction(this IMyFaction faction)
		{
			return !faction.IsNpc();
		}

		/*public static bool IsPeacefulNPC(this IMyFaction Faction)
		{
			try
			{
				if (!Faction.IsNPC()) return false;
				return Diplomacy.LawfulFactionsTags.Contains(Faction.Tag);
			}
			catch (Exception Scrap)
			{
				AISessionCore.LogError("Faction.IsPeacefulNPC", Scrap);
				return false;
			}
		}*/

		//public static float GetHealth(this IMySlimBlock block)
		//{
		//	return Math.Min(block.DamageRatio, block.BuildLevelRatio);
		//}

		public static IMyFaction FindOwnerFactionById(long identityId)
		{
			Dictionary<long, IMyFaction>.ValueCollection factions = MyAPIGateway.Session.Factions.Factions.Values;
			foreach (IMyFaction faction in factions)
			{
				if (faction.IsMember(identityId)) return faction;
			}
			return null;
		}

		//public static string Line(this string str, int lineNumber, string newlineStyle = "\r\n")
		//{
		//	return str.Split(newlineStyle.ToCharArray())[lineNumber];
		//}

		public static IMyPlayer GetPlayerById(this IMyPlayerCollection players, long playerId)
		{
			List<IMyPlayer> myPlayers = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(myPlayers, x => x.IdentityId == playerId);
			return myPlayers.FirstOrDefault();
		}

		public static bool IsValidPlayer(this IMyPlayerCollection players, long playerId, out IMyPlayer player, bool checkNonBot = true)
		{
			player = MyAPIGateway.Players.GetPlayerById(playerId);
			if (player == null) return false;
			return !checkNonBot || !player.IsBot;
		}

		//public static bool IsValidPlayer(this IMyPlayerCollection players, long playerId, bool checkNonBot = true)
		//{
		//	IMyPlayer player;
		//	return IsValidPlayer(players, playerId, out player);
		//}

        /// <summary>
        /// Returns the class name of a type, omitting all namespaces.
        /// </summary>
        public static string GetTypeName(this object obj)
        {
            return obj.GetType().ToString().Split('.').Last();
        }
	}
}