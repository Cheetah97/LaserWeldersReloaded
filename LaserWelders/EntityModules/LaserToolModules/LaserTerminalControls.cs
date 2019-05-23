using EemRdx.EntityModules;
using EemRdx.Networking;
using Sandbox.ModAPI;
using System;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface ILaserTerminalControls : IEntityModule
    {
        LaserToolPersistentStruct CurrentSettings { get; }
        bool DistanceMode { get; set; }
        bool DumpStone { get; set; }
        int LaserBeamLength { get; set; }
        int SpeedMultiplier { get; set; }
        bool ToolMode { get; set; }
    }

    public class LaserTerminalControlsModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, ILaserTerminalControls
    {
        public LaserTerminalControlsModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(LaserTerminalControlsModule);

        #region Terminal settings accessors
        public bool DistanceMode
        {
            get { return ToolSettingsSync?.Data.DistanceBased ?? PersistenceModule.Fallback.DistanceBased; }
            set
            {
                LaserToolPersistentStruct newSettings = ToolSettingsSync.Data;
                newSettings.DistanceBased = value;
                ToolSettingsSync.Set(newSettings);
            }
        }
        public int LaserBeamLength
        {
            get { return ToolSettingsSync?.Data.BeamLength ?? PersistenceModule.Fallback.BeamLength; }
            set
            {
                LaserToolPersistentStruct newSettings = ToolSettingsSync.Data;
                newSettings.BeamLength = value;
                ToolSettingsSync.Set(newSettings);
            }
        }
        public int SpeedMultiplier
        {
            get { return ToolSettingsSync?.Data.SpeedMultiplier ?? PersistenceModule.Fallback.SpeedMultiplier; }
            set
            {
                LaserToolPersistentStruct newSettings = ToolSettingsSync.Data;
                newSettings.SpeedMultiplier = value;
                ToolSettingsSync.Set(newSettings);
            }
        }

        public const bool ToolModeWelder = true;
        public const bool ToolModeGrinder = false;

        public bool ToolMode
        {
            get { return ToolSettingsSync?.Data.ToolMode ?? PersistenceModule.Fallback.ToolMode; }
            set
            {
                LaserToolPersistentStruct newSettings = ToolSettingsSync.Data;
                newSettings.ToolMode = value;
                ToolSettingsSync.Set(newSettings);
            }
        }
        public bool DumpStone
        {
            get { return ToolSettingsSync?.Data.DumpStone ?? PersistenceModule.Fallback.DumpStone; }
            set
            {
                LaserToolPersistentStruct newSettings = ToolSettingsSync.Data;
                newSettings.DumpStone = value;
                ToolSettingsSync.Set(newSettings);
            }
        }
        #endregion
        private Sync<LaserToolPersistentStruct> ToolSettingsSync;

        public bool Inited { get; private set; }

        public LaserToolPersistentStruct CurrentSettings => ToolSettingsSync?.Data ?? PersistenceModule.Fallback;

        void InitializableModule.Init()
        {
            LaserToolPersistentStruct saved = MyKernel.Persistence.Saved;

            Inited = true;
            ToolSettingsSync = new Sync<LaserToolPersistentStruct>(MyKernel.SessionBase.Networker, $"LaserTool {MyKernel.Entity.EntityId}", (MyKernel.Persistence != null ? MyKernel.Persistence.Saved : default(LaserToolPersistentStruct)), UseServersideSync: false);
            ToolSettingsSync.Register();

            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
            {
                ToolSettingsSync.Ask();
            }
            ToolSettingsSync.RegisterEventHandler(RedrawControls);
            EnsureControls();
        }

        private void EnsureControls()
        {
            if (MyKernel.Tool == null)
                throw new NullReferenceException($"MyKernel.Tool is null");

            if (MyKernel.Session.LaserToolTermControlsGenerator == null)
                throw new NullReferenceException($"MyKernel.Session.TermControlsGenerator is null");

            MyKernel.Session.LaserToolTermControlsGenerator.EnsureToolControls();
        }

        private void RedrawControls()
        {
            MyKernel.Tool.UpdateVisual();
            MyKernel.Tool.RefreshCustomInfo();
        }
    }
}
