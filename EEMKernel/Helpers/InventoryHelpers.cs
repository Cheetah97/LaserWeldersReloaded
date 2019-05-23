using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace EemRdx.Helpers
{
	public static class InventoryHelpers
	{
		public static MyDefinitionId GetBlueprint(this IMyInventoryItem item)
		{
			return new MyDefinitionId(item.Content.TypeId, item.Content.SubtypeId);
		}

		public static bool IsOfType(this MyDefinitionId id, string type)
		{
			return id.TypeId.ToString() == type || id.TypeId.ToString() == "MyObjectBuilder_" + type;
		}

		public static bool IsOfType(this MyObjectBuilder_Base id, string type)
		{
			return id.TypeId.ToString() == type || id.TypeId.ToString() == "MyObjectBuilder_" + type;
		}

		public static bool IsOfType(this IMyInventoryItem item, string type)
		{
			return item.Content.IsOfType(type);
		}
	}
}