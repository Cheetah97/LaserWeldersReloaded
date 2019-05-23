using System.Collections.Generic;
using Sandbox.ModAPI;

// ReSharper disable StringLiteralTypo

namespace EemRdx.Helpers
{
	public static class Constants
	{
		#region "General Settings"

		public static bool DebugMode { get; } = true;

		public static bool EnableProfilingLog { get; } = true;

		public static bool EnableGeneralLog { get; } = true;

		public static string DebugLogName { get; } = "EEM_Debug";

		public static string ProfilingLogName { get; } = "EEM_Profiling";

		public static string GeneralLogName { get; } = "EEM_General";

        /// <summary>
        /// This permits certain operations to throw custom exceptions in order to
        /// provide detailed descriptions of what gone wrong, over call stack.<para />
        /// BEWARE, every exception thrown must be explicitly provided with a catcher, or it will crash the entire game!
	    /// </summary>
	    public static bool AllowThrowingErrors { get; } = true;

	    public const int TicksPerSecond = 60;

	    public const int TicksPerMinute = TicksPerSecond * 60;

	    public const int DefaultLocalMessageDisplayTime = 5000;

	    public const int DefaultServerMessageDisplayTime = 10000;

        /// <summary>
        /// Returns if the code is executing on the server or locally
        /// </summary>
        public static bool IsServer => MyAPIGateway.Multiplayer.IsServer;

		#endregion
        
        #region Networking

        /// <summary>
        /// 16759
	    /// </summary>
	    public static ushort EemNetworkId { get; } = 16757;

	    public const string ServerCommsPrefix = "EEMServerMessage";

	    public const string DeclareWarMessagePrefix = "DeclareWar";

	    public const string DeclarePeaceMessagePrefix = "DeclarePeace";

	    public const string AcceptPeaceMessagePrefix = "AcceptPeace";

	    public const string RejectPeaceMessagePrefix = "RejectPeace";

	    public const string InitFactionsMessagePrefix = "InitFactions";

        #endregion
        
	}
}