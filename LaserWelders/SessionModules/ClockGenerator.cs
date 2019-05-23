using EemRdx.SessionModules;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface IClockGenerator : ISessionModule
    {
        int Ticker { get; }
    }

    public class ClockGenerator : SessionModuleBase<ILaserWeldersSessionKernel>, UpdatableModule, IClockGenerator
    {
        public ClockGenerator(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(ClockGenerator);
        public int Ticker { get; private set; } = 0;

        void UpdatableModule.Update()
        {
            Ticker++;
        }
    }
}
