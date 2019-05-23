using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EemRdx.EntityModules;
using EemRdx.Extensions;
using LoggingLevelEnum = EemRdx.SessionModules.LoggingLevelEnum;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace EemRdx
{
    public abstract class EntityKernel : MyGameLogicComponent, IEntityKernel
    {
        /// <summary>
        /// Be aware that this can be null.
        /// </summary>
        public IOperabilityProvider OperabilityProvider => GetModule<IOperabilityProvider>(); 
        public ISessionKernel SessionBase => SessionKernel.SessionBase;
        #region IEntityKernel implementation

        public T GetModule<T>() where T : IEntityModule
        {// This is going to be called often, better go with no LINQ since it can be 2x slower
            foreach (IEntityModule module in EntityModules)
            {
                if (module is T) return (T)module;
            }
            return default(T);
        }
        #endregion

        #region Entity Modules Structure
        protected HashSet<IEntityModule> EntityModules = new HashSet<IEntityModule>();

        // <Cheetah Comment> Virtual because derived types might want to change the module list and order completely
        protected virtual void CreateModules()
        {
            
        }

        protected void InitModules()
        {
            foreach (InitializableModule module in EntityModules.OfType<InitializableModule>())
            {
                try { if (!module.Inited) module.Init(); }
                catch (Exception Scrap)
                {
                    string moduleName = module.GetTypeName();
                    WriteToLog($"{DebugFullName}.InitModules:", $"Caught an exception in {moduleName}", showOnHud: true, color: "Red");
                    LogError($"{DebugFullName}.InitModules", $"in {moduleName}", Scrap);
                }
            }
        }

        protected void CloseModules()
        {
            foreach (ClosableModule module in EntityModules.OfType<ClosableModule>())
            {
                try { module.Close(); }
                catch (Exception Scrap)
                {
                    string moduleName = module.GetTypeName();
                    WriteToLog($"{DebugFullName}.CloseModules:", $"Caught an exception in {moduleName}", showOnHud: true, color: "Red");
                    LogError($"{DebugFullName}.CloseModules", $"in {moduleName}", Scrap);
                }
            }
        }

        /// <summary>
        /// Updates modules which are set to update every tick.
        /// </summary>
        protected void UpdateModules1()
        {
            if (SessionBase.SessionState == SessionStateEnum.Unloading) return;
            foreach (UpdatableModule module in EntityModules.OfType<UpdatableModule>().Where(x => x.UpdateFrequency == MyEntityUpdateEnum.EACH_FRAME))
            {
                try
                {
                    if (!module.RequiresOperable || OperabilityProvider?.CanOperate == true) module.Update();
                }
                catch (Exception Scrap)
                {
                    string moduleName = module.GetTypeName();
                    WriteToLog($"{DebugFullName}.UpdateModules1:", $"Caught an exception in {moduleName}", showOnHud: true, color: "Red");
                    LogError($"{DebugFullName}.UpdateModules1", $"in {moduleName}", Scrap);
                }
            }
        }

        /// <summary>
        /// Updates modules which are set to update every 10 ticks.
        /// </summary>
        protected void UpdateModules10()
        {
            if (SessionBase.SessionState == SessionStateEnum.Unloading) return;
            foreach (UpdatableModule module in EntityModules.OfType<UpdatableModule>().Where(x => x.UpdateFrequency == MyEntityUpdateEnum.EACH_10TH_FRAME))
            {
                try
                {
                    if (!module.RequiresOperable || OperabilityProvider?.CanOperate == true) module.Update();
                }
                catch (Exception Scrap)
                {
                    string moduleName = module.GetTypeName();
                    WriteToLog($"{DebugFullName}.UpdateModules10:", $"Caught an exception in {moduleName}", showOnHud: true, color: "Red");
                    LogError($"{DebugFullName}.UpdateModules10", $"in {moduleName}", Scrap);
                }
            }
        }

        /// <summary>
        /// Updates modules which are set to update every 100 ticks.
        /// </summary>
        protected void UpdateModules100()
        {
            if (SessionBase.SessionState == SessionStateEnum.Unloading) return;
            //WriteToLog($"BotKernel[{Grid.DisplayName}]", $"CanOperate: {OperabilityProvider?.CanOperate.ToString()}");
            foreach (UpdatableModule module in EntityModules.OfType<UpdatableModule>().Where(x => x.UpdateFrequency == MyEntityUpdateEnum.EACH_100TH_FRAME))
            {
                try
                {
                    if (!module.RequiresOperable || OperabilityProvider?.CanOperate == true) module.Update();
                }
                catch (Exception Scrap)
                {
                    string moduleName = module.GetTypeName();
                    WriteToLog($"{DebugFullName}.UpdateModules100:", $"Caught an exception in {moduleName}", showOnHud: true, color: "Red");
                    LogError($"{DebugFullName}.UpdateModules100", $"in {moduleName}", Scrap);
                }
            }
        }


        #endregion

        #region MyGameLogic implementation
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            foreach (PreinitializableModule module in EntityModules.OfType<PreinitializableModule>())
            {
                try { module.Preinit(); }
                catch (Exception Scrap)
                {
                    string moduleName = module.GetTypeName();
                    WriteToLog($"{DebugFullName}.OnAddedToContainer:", $"Caught an exception in {moduleName}", showOnHud: true, color: "Red");
                    LogError($"{DebugFullName}.OnAddedToContainer", $"in {moduleName}", Scrap);
                }
            }
            if (Entity.InScene) OnAddedToScene();
        }

        public override void OnAddedToScene()
        {
            WriteToLog($"{DebugFullName}", "Initialized", LoggingLevelEnum.DebugLog);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        // <Cheetah Comment> The idea behind using such kind of update logic is that the game
        // does distribute load between several ticks, so the updates of modules will be spread
        // as far as possible between grids
        public override void UpdateOnceBeforeFrame()
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            UpdateBeforeFirstFrame();
        }

        public override void UpdateBeforeSimulation()
        {
            UpdateModules1();
        }

        public override void UpdateBeforeSimulation10()
        {
            UpdateModules10();
        }

        public override void UpdateBeforeSimulation100()
        {
            UpdateModules100();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            Shutdown();
            if (Entity.InScene) OnRemovedFromScene();
        }
        #endregion

        #region Debug
        public string DebugFullName => string.Format("{0}[{1}]", DebugKernelName, (Entity != null ? Entity.DisplayName : "null"));
        public abstract string DebugKernelName { get; }
        protected virtual LoggingLevelEnum PreferredWritingLog { get; } = LoggingLevelEnum.Default;
        protected virtual LoggingLevelEnum PreferredErrorLog { get; } = LoggingLevelEnum.Default;

        protected void WriteToLog(string caller, string message, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.Default, bool showOnHud = false, int duration = Helpers.Constants.DefaultLocalMessageDisplayTime, string color = VRage.Game.MyFontEnum.Green, string DefaultDebugNameOverride = null)
        {
            if (DefaultDebugNameOverride == null) DefaultDebugNameOverride = DebugFullName;
            string qualifiedCaller = string.Format("{0}.{1}", DefaultDebugNameOverride, caller);
            SessionBase?.Log?.WriteToLog(qualifiedCaller, message, LoggingLevel);
            if (showOnHud)
                Utilities.Log.BuildHudNotification(qualifiedCaller, message, duration, color);
        }

        protected void LogError(string source, string message, Exception Scrap, LoggingLevelEnum LoggingLevel = LoggingLevelEnum.Default, string DefaultDebugNameOverride = null)
        {
            if (DefaultDebugNameOverride == null) DefaultDebugNameOverride = DebugFullName;
            string qualifiedSource = string.Format("{0}.{1}", DefaultDebugNameOverride, source);
            SessionBase?.Log?.LogError(qualifiedSource, message, Scrap, LoggingLevel);
        }
        #endregion

        public EntityKernel()
        {
            CreateModules();
        }

        protected void UpdateBeforeFirstFrame()
        {
            EnumerateModules();
            KernelInit();
            InitModules();
            KernelPostInit();
        }

        protected void EnumerateModules()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Got {EntityModules.Count} modules:");
            foreach (IEntityModule module in EntityModules)
            {
                string moduleName = module.GetTypeName();
                builder.Append($"{moduleName} ");
            }
            WriteToLog($"{DebugFullName}.InitModules", builder.ToString(), LoggingLevelEnum.DebugLog);
        }

        /// <summary>
        /// Place for additional kernel initialization logic. Executed before module-init.
        /// </summary>
        protected virtual void KernelInit() { }

        /// <summary>
        /// Place for additional kernel initialization logic. Executed after module-init.
        /// </summary>
        protected virtual void KernelPostInit() { }

        public void Shutdown()
        {
            CloseModules();
        }
    }
}
