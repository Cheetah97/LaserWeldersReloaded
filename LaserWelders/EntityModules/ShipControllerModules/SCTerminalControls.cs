using EemRdx.EntityModules;
using EemRdx.Networking;
using Sandbox.ModAPI;
using System;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public interface ISCTerminalControls : IEntityModule
    {
        SCPersistentStruct CurrentSettings { get; }
        float HudPosX { get; set; }
        float HudPosY { get; set; }
        bool ShowHud { get; set; }
        bool VerboseHud { get; set; }
    }

    public class SCTerminalControlsModule : EntityModuleBase<ISCKernel>, InitializableModule, ISCTerminalControls
    {
        public SCTerminalControlsModule(ISCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(SCTerminalControlsModule);

        #region Terminal settings accessors
        public bool ShowHud
        {
            get { return SCSettingsSync?.Data.ShowHud ?? SCPersistenceModule.Fallback.ShowHud; }
            set
            {
                SCPersistentStruct newSettings = SCSettingsSync.Data;
                newSettings.ShowHud = value;
                SCSettingsSync.Set(newSettings);
            }
        }
        public bool VerboseHud
        {
            get { return SCSettingsSync?.Data.VerboseHud ?? SCPersistenceModule.Fallback.VerboseHud; }
            set
            {
                SCPersistentStruct newSettings = SCSettingsSync.Data;
                newSettings.VerboseHud = value;
                SCSettingsSync.Set(newSettings);
            }
        }
        public float HudPosX
        {
            get { return SCSettingsSync?.Data.HudPosX ?? SCPersistenceModule.Fallback.HudPosX; }
            set
            {
                SCPersistentStruct newSettings = SCSettingsSync.Data;
                newSettings.HudPosX = value;
                SCSettingsSync.Set(newSettings);
            }
        }
        public float HudPosY
        {
            get { return SCSettingsSync?.Data.HudPosY ?? SCPersistenceModule.Fallback.HudPosY; }
            set
            {
                SCPersistentStruct newSettings = SCSettingsSync.Data;
                newSettings.HudPosY = value;
                SCSettingsSync.Set(newSettings);
            }
        }
        #endregion
        private Sync<SCPersistentStruct> SCSettingsSync;

        public bool Inited { get; private set; }

        public SCPersistentStruct CurrentSettings => SCSettingsSync?.Data ?? SCPersistenceModule.Fallback;

        void InitializableModule.Init()
        {
            SCPersistentStruct saved = MyKernel.Persistence.Saved;

            Inited = true;
            SCSettingsSync = new Sync<SCPersistentStruct>(MyKernel.SessionBase.Networker, $"ShipController {MyKernel.Entity.EntityId}", (MyKernel.Persistence != null ? MyKernel.Persistence.Saved : default(SCPersistentStruct)), UseServersideSync: false);
            SCSettingsSync.Register();

            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
            {
                SCSettingsSync.Ask();
            }
            SCSettingsSync.RegisterEventHandler(RedrawControls);
            EnsureControls();
        }

        private void EnsureControls()
        {
            if (MyKernel.Block is IMyCockpit)
                MyKernel.Session.SCTermControlsGenerator.EnsureCockpitControls();
            if (MyKernel.Block is IMyRemoteControl)
                MyKernel.Session.SCTermControlsGenerator.EnsureRCControls();
        }

        private void RedrawControls()
        {
            MyKernel.Block.UpdateVisual();
            MyKernel.Block.RefreshCustomInfo();
        }
    }
}
