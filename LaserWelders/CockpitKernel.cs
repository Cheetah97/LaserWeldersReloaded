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
    public interface ISCKernel : IEntityKernel
    {
        ILaserWeldersSessionKernel Session { get; }
        IMyTerminalBlock Block { get; }
        IMyShipController SC { get; }
        IToolListProvider ToolListProvider { get; }
        ISCPersistence Persistence { get; }
        ISCTerminalControls TermControls { get; }
    }

    public class SCKernel : EntityKernel, ISCKernel
    {
        public override string DebugKernelName { get; } = nameof(SCKernel);

        public ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
        public IMyTerminalBlock Block => Entity as IMyTerminalBlock;
        public IMyShipController SC => Entity as IMyShipController;
        public IToolListProvider ToolListProvider => GetModule<IToolListProvider>();
        public ISCPersistence Persistence => GetModule<ISCPersistence>();
        public ISCTerminalControls TermControls => GetModule<ISCTerminalControls>();

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new SCPersistenceModule(this));
            EntityModules.Add(new SCTerminalControlsModule(this));
            EntityModules.Add(new ToolListProvider(this));
            EntityModules.Add(new HUDModule(this));
        }

        public SCKernel() { }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class CockpitKernel : SCKernel { }
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RemoteControl), false)]
    public class RCKernel : SCKernel { }
}
