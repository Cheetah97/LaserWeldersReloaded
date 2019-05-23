using System;
using System.IO;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace EemRdx.Helpers
{
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public static class ShowIngameMessage
	{
		private static string SessionLogName { get; } = $"EEMRdxSessionLog--{DateTime.Now:HHmmssss}.log";

		private static TextWriter SessionWriter { get; } = MyAPIGateway.Utilities.WriteFileInLocalStorage(SessionLogName, typeof(ShowIngameMessage));

		private static string RcLogName { get; } = $"EEMRdxRCLog--{DateTime.Now:HHmmssss}.log";

		private static TextWriter RcWriter { get; } = MyAPIGateway.Utilities.WriteFileInLocalStorage(RcLogName, typeof(ShowIngameMessage));

		public static void ShowOverrideMessage(string message)
		{
			MyVisualScriptLogicProvider.ShowNotificationToAll(message, 20000);
			//SessionWriter.WriteLine(message);
		}


		public static void ShowSessionMessage(string message)
		{
			if (!Constants.DebugMode) return;
			MyVisualScriptLogicProvider.ShowNotificationToAll(message, 20000);
			//SessionWriter.WriteLine(message);
		}

		public static void ShowRcMessage(string message)
		{
			if (!Constants.DebugMode) return;
			MyVisualScriptLogicProvider.ShowNotificationToAll(message, 20000);
			RcWriter.WriteLine(message);
		}
	}
}
