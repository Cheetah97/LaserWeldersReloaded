namespace EemRdx.SessionModules
{
    public interface ISessionModule
    {
        string DebugModuleName { get; }
    }
    public interface InitializableModule : ISessionModule
    {
        void Init();
    }
    public interface UpdatableModule : ISessionModule
    {
        void Update();
    }
    public interface UnloadableModule : ISessionModule
    {
        void UnloadData();
    }
    public interface SaveableModule : ISessionModule
    {
        void Save();
    }
    public interface LoadableModule : ISessionModule
    {
        void LoadData();
    }

    public abstract class SessionModuleBase<TKernel> : ISessionModule where TKernel: ISessionKernel
    {
        public TKernel MySessionKernel { get; private set; }
        public SessionModuleBase(TKernel MySessionKernel)
        {
            this.MySessionKernel = MySessionKernel;
        }

        public abstract string DebugModuleName { get; }
        protected void WriteToLog(string caller, string message, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.DebugLog, bool showOnHud = false, int duration = Helpers.Constants.DefaultLocalMessageDisplayTime, string color = VRage.Game.MyFontEnum.Green, string DefaultDebugNameOverride = null)
        {
            if (DefaultDebugNameOverride == null) DefaultDebugNameOverride = DebugModuleName;
            string qualifiedCaller = !string.IsNullOrWhiteSpace(caller) ? string.Format("{0}.{1}", DefaultDebugNameOverride, caller) : DefaultDebugNameOverride;
            MySessionKernel?.Log?.WriteToLog(qualifiedCaller, message, LoggingLevel);
            if (showOnHud)
                Utilities.Log.BuildHudNotification(qualifiedCaller, message, duration, color);
        }

        protected void LogError(string source, string message, System.Exception Scrap, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.GeneralLog, string DefaultDebugNameOverride = null)
        {
            if (DefaultDebugNameOverride == null) DefaultDebugNameOverride = DebugModuleName;
            string qualifiedSource = string.Format("{0}.{1}", DefaultDebugNameOverride, source);
            MySessionKernel?.Log?.LogError(qualifiedSource, message, Scrap, LoggingLevel);
        }
    }
}
