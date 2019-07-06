using EemRdx;
using EemRdx.EntityModules;
using EemRdx.LaserWelders.EntityModules.LaserToolModules;
using EemRdx.SessionModules;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;

namespace EemRdx.LaserWelders
{
    public interface ILaserToolKernel : ITerminalBlockKernel<ILaserWeldersSessionKernel, IMyShipWelder, LaserToolPersistentStruct, LaserToolPersistenceModule, LaserTerminalControlsModule>
    {
        IMyShipWelder Tool { get; }

        IToggle Toggle { get; }
        IBeamDrawer BeamDrawer { get; }
        IInventory Inventory { get; }
        IPowerModule PowerModule { get; }
        IResponder Responder { get; }
        ICombatAbusePreventionModule CombatAbusePrevention { get; }
        IConcealmentDetectionModule ConcealmentDetectionModule { get; }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipWelder), false, "LargeShipLaserMultitool", "SmallShipLaserMultitool", "LargeShipLaserMultitoolMK2", "SmallShipLaserMultitoolMK2")]
    public class LaserToolKernel : LaserWeldersTerminalBlockKernelBase<IMyShipWelder, LaserToolPersistentStruct, LaserToolPersistenceModule, LaserTerminalControlsModule>, ILaserToolKernel
    {
        public override string DebugKernelName { get; } = nameof(LaserToolKernel);

        public IMyShipWelder Tool => TypedBlock;

        public IToggle Toggle => GetModule<IToggle>();
        public IBeamDrawer BeamDrawer => GetModule<IBeamDrawer>();
        public IInventory Inventory => GetModule<IInventory>();
        public IPowerModule PowerModule => GetModule<IPowerModule>();
        public IResponder Responder => GetModule<IResponder>();
        public IResponderModuleInternal ResponderModule => GetModule<IResponderModuleInternal>();
        public ICombatAbusePreventionModule CombatAbusePrevention => GetModule<ICombatAbusePreventionModule>();
        public IConcealmentDetectionModule ConcealmentDetectionModule => GetModule<IConcealmentDetectionModule>();

        //protected override LoggingLevelEnum PreferredWritingLog { get; } = LoggingLevelEnum.DebugLog;

        protected override void CreateModules()
        {
            EntityModules.Add(new LaserToolTermControlsHelperModule(this));
            base.CreateModules();
            EntityModules.Add(new ConcealmentDetectionModule(this));
            EntityModules.Add(new PowerModule(this));
            EntityModules.Add(new CombatAbusePreventionModule(this));
            EntityModules.Add(new ToolOperabilityProvider(this));
            EntityModules.Add(new ToggleModule(this));
            EntityModules.Add(new LaserToolTermControlsHelperModule(this));
            EntityModules.Add(new BeamDrawerModule(this));
            EntityModules.Add(new EmissivesModule(this));
            EntityModules.Add(new InventoryModule(this));
            EntityModules.Add(new WorkingModule(this));
            EntityModules.Add(new ResponderModule(this));
            EntityModules.Add(new ToolInventoryCleaner(this));
        }

        protected override LaserToolPersistenceModule CreatePersistenceModule()
        {
            return new LaserToolPersistenceModule(this);
        }

        protected override LaserTerminalControlsModule CreateTerminalControlsModule()
        {
            return new LaserTerminalControlsModule(this);
        }

        protected override void KernelPostInit()
        {
            base.KernelPostInit();
            WriteToLog("KernelPostinit", $"Owner ID: {Block.OwnerId}", LoggingLevelEnum.DebugLog);
        }
    }
}
