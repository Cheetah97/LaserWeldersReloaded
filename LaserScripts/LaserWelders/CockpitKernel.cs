using System;
using EemRdx;
using EemRdx.EntityModules;
using EemRdx.LaserWelders.EntityModules;
using EemRdx.LaserWelders.EntityModules.ShipControllerModules;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace EemRdx.LaserWelders
{
    public interface ISCKernel : ITerminalBlockKernel<ILaserWeldersSessionKernel, IMyShipController, SCPersistentStruct, SCPersistenceModule, SCTerminalControlsModule>
    {
        IMyShipController SC { get; }
        IToolListProvider ToolListProvider { get; }
        IHUDModule HUDModule { get; }
        //IGPSMarkerModule GPSMarkerModule { get; }
    }

    public abstract class SCKernel : LaserWeldersTerminalBlockKernelBase<IMyShipController, SCPersistentStruct, SCPersistenceModule, SCTerminalControlsModule>, ISCKernel
    {
        public override string DebugKernelName { get; } = nameof(SCKernel);

        public override ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
        public IMyShipController SC => TypedBlock;
        public IToolListProvider ToolListProvider => GetModule<IToolListProvider>();
        //public IGPSMarkerModule GPSMarkerModule => GetModule<IGPSMarkerModule>();
        public IHUDModule HUDModule => GetModule<IHUDModule>();

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(GetTermHelperModule());
            EntityModules.Add(new ToolListProvider(this));
            EntityModules.Add(new HUDModule(this));
            EntityModules.Add(new GridAnalyzerModule(this));
            //EntityModules.Add(new GPSMarkerModule(this));
        }

        protected abstract ITerminalControlsHelperModule GetTermHelperModule();

        protected override SCPersistenceModule CreatePersistenceModule()
        {
            return new SCPersistenceModule(this);
        }

        protected override SCTerminalControlsModule CreateTerminalControlsModule()
        {
            return new SCTerminalControlsModule(this);
        }

        public SCKernel() { }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class CockpitKernel : SCKernel
    {
        protected override ITerminalControlsHelperModule GetTermHelperModule()
        {
            return new CockpitTermControlsHelperModule(this);
        }
    }
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RemoteControl), false)]
    public class RCKernel : SCKernel
    {
        protected override ITerminalControlsHelperModule GetTermHelperModule()
        {
            return new RCTermControlsHelperModule(this);
        }
    }
}
