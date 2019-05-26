using EemRdx.LaserWelders.EntityModules.LaserToolModules;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace EemRdx.LaserWelders
{
    public interface ILaserToolKernel : IEntityKernel
    {
        ILaserWeldersSessionKernel Session { get; }
        IMyFunctionalBlock Block { get; }
        IMyShipToolBase Tool { get; }

        IToggle Toggle { get; }
        ILaserTerminalControls TermControls { get; }
        IBeamDrawer BeamDrawer { get; }
        IPersistence Persistence { get; }
        IInventory Inventory { get; }
        IPowerModule PowerModule { get; }
        IResponder Responder { get; }
        ICombatAbusePreventionModule CombatAbusePrevention { get; }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipGrinder), false, "LargeShipLaserMultitool", "SmallShipLaserMultitool", "LargeShipLaserMultitoolMK2", "SmallShipLaserMultitoolMK2")]
    public class LaserToolKernel : EntityKernel, ILaserToolKernel
    {
        public override string DebugKernelName { get; } = nameof(LaserToolKernel);

        public ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
        public IMyFunctionalBlock Block => Entity as IMyFunctionalBlock;
        public IMyShipToolBase Tool => Entity as IMyShipToolBase;

        public IToggle Toggle => GetModule<IToggle>();
        public ILaserTerminalControls TermControls => GetModule<ILaserTerminalControls>();
        public IBeamDrawer BeamDrawer => GetModule<IBeamDrawer>();
        public IPersistence Persistence => GetModule<IPersistence>();
        public IInventory Inventory => GetModule<IInventory>();
        public IPowerModule PowerModule => GetModule<IPowerModule>();
        public IResponder Responder => GetModule<IResponder>();
        public IResponderModuleInternal ResponderModule => GetModule<IResponderModuleInternal>();
        public ICombatAbusePreventionModule CombatAbusePrevention => GetModule<ICombatAbusePreventionModule>();

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new PersistenceModule(this));
            EntityModules.Add(new PowerModule(this));
            EntityModules.Add(new CombatAbusePreventionModule(this));
            EntityModules.Add(new ToolOperabilityProvider(this));
            EntityModules.Add(new ToggleModule(this));
            EntityModules.Add(new LaserTerminalControlsModule(this));
            EntityModules.Add(new BeamDrawerModule(this));
            EntityModules.Add(new EmissivesModule(this));
            EntityModules.Add(new InventoryModule(this));
            EntityModules.Add(new WorkingModule(this));
            EntityModules.Add(new ResponderModule(this));
        }

        protected override void KernelPostInit()
        {
            WriteToLog("KernelPostinit", $"Owner ID: {Block.OwnerId}");
        }
    }
}
