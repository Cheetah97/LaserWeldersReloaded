using LoggingLevelEnum = EemRdx.SessionModules.LoggingLevelEnum;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

//using Cheetah.AI;

namespace EemRdx.EntityModules
{
    public abstract class EntityModuleBase<TKernel> : IEntityModule where TKernel: IEntityKernel
    {
        public TKernel MyKernel { get; private set; }
        protected IMyEntity MyEntity => MyKernel.Entity;

        public EntityModuleBase(TKernel MyKernel)
        {
            this.MyKernel = MyKernel;
        }

        public string DebugFullName => string.Format("{0}[{1}]", DebugModuleName, (MyEntity != null ? MyEntity.DisplayName : "null"));
        public abstract string DebugModuleName { get; }

        #region Debug writing
        protected void WriteToLog(string caller, string message, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.Default, bool showOnHud = false, int duration = Helpers.Constants.DefaultLocalMessageDisplayTime, string color = VRage.Game.MyFontEnum.Green, string DefaultDebugNameOverride = null)
        {
            if (DefaultDebugNameOverride == null) DefaultDebugNameOverride = DebugFullName;
            string qualifiedCaller = !string.IsNullOrWhiteSpace(caller) ? string.Format("{0}.{1}", DefaultDebugNameOverride, caller) : DefaultDebugNameOverride;
            if (MyKernel.SessionBase != null && MyKernel.SessionBase.Log != null)
                MyKernel.SessionBase.Log.WriteToLog(qualifiedCaller, message, LoggingLevel);
            else
            {
                VRage.Utils.MyLog.Default.WriteLine($"{Utilities.Log.TimeStamp}{Utilities.Log.Indent}{caller}{Utilities.Log.Indent}{message}");
            }
            if (showOnHud)
                Utilities.Log.BuildHudNotification(qualifiedCaller, message, duration, color);
        }

        protected void LogError(string source, string message, System.Exception Scrap, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.Default, string DefaultDebugNameOverride = null)
        {
            if (DefaultDebugNameOverride == null) DefaultDebugNameOverride = DebugFullName;
            string qualifiedSource = string.Format("{0}.{1}", DefaultDebugNameOverride, source);
            if (MyKernel.SessionBase != null && MyKernel.SessionBase.Log != null)
                MyKernel.SessionBase.Log?.LogError(qualifiedSource, message, Scrap, LoggingLevel);
            else
            {
                VRage.Utils.MyLog.Default.WriteLine($"{Utilities.Log.TimeStamp}{Utilities.Log.Indent}{source}{Utilities.Log.Indent}{message}");
            }
        }
        #endregion
    }

    public interface PreinitializableModule : IEntityModule
    {
        void Preinit();
    }

    public interface InitializableModule : IEntityModule
    {
        bool Inited { get; }
        void Init();
    }

    public interface ClosableModule : IEntityModule
    {
        void Close();
    }

    public interface UpdatableModule : IEntityModule
    {
        /// <summary>
        /// Determines whether the module requires the bot grid to be operable
        /// in order to perform updates.
        /// </summary>
        bool RequiresOperable { get; }
        VRage.ModAPI.MyEntityUpdateEnum UpdateFrequency { get; }
        void Update();
    }
}
