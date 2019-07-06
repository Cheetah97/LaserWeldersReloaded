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
        //IBlockLimitsProvider BlockLimits { get; }
        IHUDAPIProvider HUDAPIProvider { get; }
        IGasPowerDensityProvider GasPowerDensityProvider { get; }
        IPerformanceLimiterModule PerformanceLimiter { get; }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class LaserWeldersSessionKernel : SessionKernel, ILaserWeldersSessionKernel
    {
        public override uint ModID { get; } = 927381544;

        public override Guid StorageGuid { get; } = new Guid("22125116-4EE3-4F87-B6D6-AE1232014EA5");
        private SettingsProviderModule SettingsProvider => GetModule<SettingsProviderModule>();
        public LaserSettings Settings => (SettingsProvider != null ? SettingsProvider.Settings : LaserSettings.Default);
        //public IBlockLimitsProvider BlockLimits => GetModule<IBlockLimitsProvider>();
        public IHUDAPIProvider HUDAPIProvider => GetModule<IHUDAPIProvider>();
        public IGasPowerDensityProvider GasPowerDensityProvider => GetModule<IGasPowerDensityProvider>();
        public IPerformanceLimiterModule PerformanceLimiter => GetModule<IPerformanceLimiterModule>();
        public static LaserWeldersSessionKernel LaserWeldersSession { get; private set; }

        protected override void CreateModules()
        {
            base.CreateModules();
            Modules.Add(new SettingsProviderModule(this));
            Modules.Add(new LaserToolTermControlsGeneratorModule(this));
            Modules.Add(new CockpitTermControlsGeneratorModule(this));
            Modules.Add(new RCTermControlsGeneratorModule(this));
            Modules.Add(new PyroboltTermControlsGeneratorModule(this));
            Modules.Add(new ProjectorTermControlsGeneratorModule(this));
            Modules.Add(new JumpTerminalControlsGenerator(this));
            //Modules.Add(new BlockLimitsProviderModule(this));
            Modules.Add(new PerformanceLimiterModule(this));
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