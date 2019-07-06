using EemRdx.EntityModules;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public interface ISCPersistence : IPersistence<SCPersistentStruct> { }

    public class SCPersistenceModule : PersistenceModuleBase<ILaserWeldersSessionKernel, IMyShipController, ISCKernel, SCPersistentStruct, SCPersistenceModule, SCTerminalControlsModule>
    {
        public SCPersistenceModule(ISCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(SCPersistenceModule);

        public override SCPersistentStruct Default { get; } = new SCPersistentStruct
        {
            ShowHud = true,
            ShowBlocksOnHud = true,
            HudPosX = -0.95f,
            HudPosY = 0.95f,
            VerboseHud = false,
        };
    }

    #region Struct
    [ProtoContract]
    public struct SCPersistentStruct : IPersistentStruct
    {
        [ProtoMember(1)]
        public bool ShowHud;
        [ProtoMember(2)]
        public float HudPosX;
        [ProtoMember(3)]
        public float HudPosY;
        [ProtoMember(4)]
        public bool VerboseHud;
        [ProtoMember(5)]
        public bool ShowBlocksOnHud;
    }
    #endregion
}
