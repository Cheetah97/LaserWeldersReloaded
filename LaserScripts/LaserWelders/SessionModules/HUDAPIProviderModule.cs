using EemRdx.SessionModules;
using Draygo.API;
using Sandbox.ModAPI;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface IHUDAPIProvider : ISessionModule
    {
        HudAPIv2 HudAPI { get; }
    }

    public class HUDAPIProviderModule : SessionModuleBase<ILaserWeldersSessionKernel>, InitializableModule, IHUDAPIProvider
    {
        public HUDAPIProviderModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(HUDAPIProviderModule);
        public HudAPIv2 HudAPI { get; private set; }

        void InitializableModule.Init()
        {
            HudAPI = new HudAPIv2();
        }
    }
}
