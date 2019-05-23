using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace EemRdx.Helpers
{
	public static class DebugHelper
	{
		private static readonly List<int> AlreadyPostedMessages = new List<int>();

		public static void Print(string source, string message, bool antiSpam = true)
		{
			string combined = source + ": " + message;
			int hash = combined.GetHashCode();

			if (AlreadyPostedMessages.Contains(hash)) return;
			AlreadyPostedMessages.Add(hash);
			MyAPIGateway.Utilities.ShowMessage(source, message);
			VRage.Utils.MyLog.Default.WriteLine(source + $": Debug message: {message}");
			VRage.Utils.MyLog.Default.Flush();
		}

		public static void DebugWrite(this IMyCubeGrid grid, string source, string message, bool antiSpam = true, bool forceWrite = false)
		{
			if (Constants.DebugMode || forceWrite) Print(grid.DisplayName, $"Debug message from '{source}': {message}");
		}

		public static void LogError(this IMyCubeGrid grid, string source, Exception scrap, bool antiSpam = true, bool forceWrite = false)
		{
			if (!Constants.DebugMode && !forceWrite) return;
			string displayName = "Unknown Grid";
			try
			{
				displayName = grid.DisplayName;
			}
			finally
			{
				Print(displayName, $"Fatal error in '{source}': {scrap.Message}. {(scrap.InnerException != null ? scrap.InnerException.Message : "No additional info was given by the game :(")}");
			}
		}
	}
}