using EemRdx.EntityModules;
using EemRdx.LaserWelders.SessionModules;
using EemRdx.Networking;
using Sandbox.ModAPI;
using System;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public interface ISCTerminalControls : ITerminalControls<SCPersistentStruct>
    {
        float HudPosX { get; set; }
        float HudPosY { get; set; }
        bool ShowHud { get; set; }
        bool ShowBlocksOnHud { get; set; }
        bool VerboseHud { get; set; }
    }

    public class SCTerminalControlsModule : TerminalControlsModuleBase<ILaserWeldersSessionKernel, IMyShipController, ISCKernel, SCPersistentStruct, SCPersistenceModule, SCTerminalControlsModule>, ISCTerminalControls
    {
        public SCTerminalControlsModule(ISCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(SCTerminalControlsModule);

        #region Terminal settings accessors
        public bool ShowHud
        {
            get { return CurrentSettings.ShowHud; }
            set
            {
                SCPersistentStruct newSettings = BlockSettingsSync.Data;
                newSettings.ShowHud = value;
                BlockSettingsSync.Set(newSettings);
            }
        }
        public bool ShowBlocksOnHud
        {
            get { return CurrentSettings.ShowBlocksOnHud; }
            set
            {
                SCPersistentStruct newSettings = BlockSettingsSync.Data;
                newSettings.ShowBlocksOnHud = value;
                BlockSettingsSync.Set(newSettings);
            }
        }
        public bool VerboseHud
        {
            get { return CurrentSettings.VerboseHud; }
            set
            {
                SCPersistentStruct newSettings = BlockSettingsSync.Data;
                newSettings.VerboseHud = value;
                BlockSettingsSync.Set(newSettings);
            }
        }
        public float HudPosX
        {
            get { return CurrentSettings.HudPosX; }
            set
            {
                SCPersistentStruct newSettings = BlockSettingsSync.Data;
                newSettings.HudPosX = value;
                BlockSettingsSync.Set(newSettings);
            }
        }
        public float HudPosY
        {
            get { return CurrentSettings.HudPosY; }
            set
            {
                SCPersistentStruct newSettings = BlockSettingsSync.Data;
                newSettings.HudPosY = value;
                BlockSettingsSync.Set(newSettings);
            }
        }
        #endregion
        
    }

    public class CockpitTermControlsHelperModule : TerminalControlsHelperModuleBase<SCKernel, CockpitTermControlsGeneratorModule>
    {
        public CockpitTermControlsHelperModule(SCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(CockpitTermControlsHelperModule);
    }

    public class RCTermControlsHelperModule : TerminalControlsHelperModuleBase<SCKernel, RCTermControlsGeneratorModule>
    {
        public RCTermControlsHelperModule(SCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(RCTermControlsHelperModule);
    }
}
