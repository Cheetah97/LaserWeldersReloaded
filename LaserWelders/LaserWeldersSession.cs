using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EemRdx.EntityModules;
using EemRdx.Extensions;
using EemRdx.LaserWelders.SessionModules;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.LaserWelders
{
    public interface ILaserWeldersSessionKernel : ISessionKernel
    {
        LaserSettings Settings { get; }
        ILaserToolTermControlsGenerator LaserToolTermControlsGenerator { get; }
        ISCTermControlsGenerator SCTermControlsGenerator { get; }
        IBlockLimitsProvider BlockLimits { get; }
        IClockGenerator Clock { get; }
        IHUDAPIProvider HUDAPIProvider { get; }
        IGasPowerDensityProvider GasPowerDensityProvider { get; }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class LaserWeldersSessionKernel : SessionKernel, ILaserWeldersSessionKernel
    {
        public override uint ModID { get; } = 927381544;

        public override Guid StorageGuid { get; } = new Guid("22125116-4EE3-4F87-B6D6-AE1232014EA5");
        private SettingsProviderModule SettingsProvider => GetModule<SettingsProviderModule>();
        public LaserSettings Settings => (SettingsProvider != null ? SettingsProvider.Settings : LaserSettings.Default);
        public ILaserToolTermControlsGenerator LaserToolTermControlsGenerator => GetModule<ILaserToolTermControlsGenerator>();
        public ISCTermControlsGenerator SCTermControlsGenerator => GetModule<ISCTermControlsGenerator>();
        public IBlockLimitsProvider BlockLimits => GetModule<IBlockLimitsProvider>();
        public IClockGenerator Clock => GetModule<IClockGenerator>();
        public IHUDAPIProvider HUDAPIProvider => GetModule<IHUDAPIProvider>();
        public IGasPowerDensityProvider GasPowerDensityProvider => GetModule<IGasPowerDensityProvider>();
        public static LaserWeldersSessionKernel LaserWeldersSession { get; private set; }

        protected override void CreateModules()
        {
            base.CreateModules();
            Modules.Add(new ClockGenerator(this));
            Modules.Add(new SettingsProviderModule(this));
            Modules.Add(new LaserToolTermControlsGeneratorModule(this));
            Modules.Add(new SCTermControlsGeneratorModule(this));
            Modules.Add(new BlockLimitsProviderModule(this));
            Modules.Add(new HUDAPIProviderModule(this));
            Modules.Add(new GasPowerDensityProviderModule(this));
        }

        public LaserWeldersSessionKernel() : base()
        {
            SessionBase = this;
            LaserWeldersSession = this;
        }
    }
}
