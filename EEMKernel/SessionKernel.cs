using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace EemRdx
{
    //[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public abstract class SessionKernel : MySessionComponentBase, ISessionKernel
    {
        private bool Inited = false;
        protected List<ISessionModule> Modules = new List<ISessionModule>();

        /// <summary>
        /// The Mod ID of your workshop upload. You may use 0 for local testing, but be sure to change after upload.
        /// </summary>
        public abstract uint ModID { get; }
        public abstract Guid StorageGuid { get; }

        public ILogProvider Log => GetModule<LogProviderModule>();
        public INetworker Networker => GetModule<NetworkerModule>();
        public ISaveProvider SaveProvider => GetModule<ISaveProvider>();
        public SessionStateEnum SessionState { get; private set; }
        public static ISessionKernel SessionBase { get; protected set; }
        public bool DebugEnabled { get; } = true;

        public T GetModule<T>() where T: ISessionModule
        {// This is going to be called often, better go with no LINQ since it can be 2x slower
            foreach (ISessionModule module in Modules)
            {
                if (module is T) return (T)module;
            }
            return default(T);
        }

        public override void UpdateBeforeSimulation()
        {
            if (!Inited && MyAPIGateway.Session != null) Init();
            UpdateModules();
        }

        private void Init()
        {
            EnumerateModules();
            KernelPreinit();
            InitModules();
            Inited = true;
            SessionState = SessionStateEnum.Running;
            KernelPostinit();
            VRage.Utils.MyLog.Default.WriteLine($"SessionKernel inited");
            VRage.Utils.MyLog.Default.Flush();
        }

        protected virtual void KernelPreinit() { }
        protected virtual void KernelPostinit() { }

        public override void LoadData()
        {
            SessionState = SessionStateEnum.Loading;
            KernelPreload();
            CreateModules();
            LoadModules();
            KernelPostload();
        }

        protected virtual void KernelPreload() { }
        protected virtual void KernelPostload() { }

        public override void SaveData()
        {
            SaveModules();
        }

        protected override void UnloadData()
        {
            SessionState = SessionStateEnum.Unloading;
            UnloadModules();
            SessionBase = null;
        }

        /// <summary>
        /// Remember to call the base when overriding
        /// </summary>
        protected virtual void CreateModules()
        {
            Modules.Add(new LogProviderModule(this));
            Modules.Add(new NetworkerModule(this));
            Modules.Add(new SaveProvider(this));
        }

        private void LoadModules()
        {
            foreach (LoadableModule module in Modules.OfType<LoadableModule>())
            {
                try
                {
                    module.LoadData();
                }
                catch (Exception Scrap)
                {
                    string funcName = "LoadModules";
                    Log?.GeneralLog?.LogError($"{this.GetType().ToString()}.{funcName}()", $"{module.DebugModuleName}.{funcName}() threw", Scrap);
                }
            }
        }

        private void InitModules()
        {
            foreach (InitializableModule module in Modules.OfType<InitializableModule>())
            {
                try
                {
                    module.Init();
                }
                catch (Exception Scrap)
                {
                    string funcName = "InitModules";
                    Log?.GeneralLog?.LogError($"{this.GetType().ToString()}.{funcName}()", $"{module.DebugModuleName}.{funcName}() threw", Scrap);
                }
            }
        }

        private void UpdateModules()
        {
            foreach (UpdatableModule module in Modules.OfType<UpdatableModule>())
            {
                try
                {
                    module.Update();
                }
                catch (Exception Scrap)
                {
                    string funcName = "UpdateModules";
                    Log?.GeneralLog?.LogError($"{this.GetType().ToString()}.{funcName}()", $"{module.DebugModuleName}.{funcName}() threw", Scrap);
                }
            }
        }

        private void UnloadModules()
        {
            foreach (UnloadableModule module in Modules.OfType<UnloadableModule>())
            {
                try
                {
                    module.UnloadData();
                }
                catch (Exception Scrap)
                {
                    string funcName = "UnloadModules";
                    Log?.GeneralLog?.LogError($"{this.GetType().ToString()}.{funcName}()", $"{module.DebugModuleName}.{funcName}() threw", Scrap);
                }
            }
        }

        private void SaveModules()
        {
            foreach (SaveableModule module in Modules.OfType<SaveableModule>())
            {
                try
                {
                    module.Save();
                }
                catch (Exception Scrap)
                {
                    string funcName = "SaveModules";
                    Log?.GeneralLog?.LogError($"{this.GetType().ToString()}.{funcName}()", $"{module.DebugModuleName}.{funcName}() threw", Scrap);
                }
            }
        }

        protected void EnumerateModules()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Got {Modules.Count} modules:");
            foreach (ISessionModule module in Modules)
            {
                string moduleName = module.GetTypeName();
                builder.Append($"{moduleName} ");
            }
            WriteToLog($"{DebugName}.InitModules", builder.ToString(), LoggingLevelEnum.DebugLog);
        }

        protected void WriteToLog(string caller, string message, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.DebugLog, bool showOnHud = false, int duration = Helpers.Constants.DefaultLocalMessageDisplayTime, string color = VRage.Game.MyFontEnum.Green, string DefaultDebugNameOverride = null)
        {
            string qualifiedCaller = !string.IsNullOrWhiteSpace(caller) ? string.Format("{0}.{1}", DefaultDebugNameOverride, caller) : DefaultDebugNameOverride;
            Log?.WriteToLog(qualifiedCaller, message, LoggingLevel);
            if (showOnHud)
                Utilities.Log.BuildHudNotification(qualifiedCaller, message, duration, color);
        }

        protected void LogError(string source, string message, Exception Scrap, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.GeneralLog, string DefaultDebugNameOverride = null)
        {
            string qualifiedSource = string.Format("{0}.{1}", DefaultDebugNameOverride, source);
            Log?.LogError(qualifiedSource, message, Scrap, LoggingLevel);
        }

        public SessionKernel()
        {
            SessionBase = this;
        }
    }
}
