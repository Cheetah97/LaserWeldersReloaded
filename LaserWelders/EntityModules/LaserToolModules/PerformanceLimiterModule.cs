using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Utils;
using EemRdx.EntityModules;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using System.Collections.Concurrent;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IPerformanceLimiter : IEntityModule
    {

    }

    public class PerformanceLimiterModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, ClosableModule, UpdatableModule, IPerformanceLimiter
    {
        public PerformanceLimiterModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(PerformanceLimiterModule);

        public bool Inited { get; } = false;

        public bool RequiresOperable { get; } = false;

        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;

        private static ConcurrentDictionary<long, ILaserToolKernel> AllTools = new ConcurrentDictionary<long, ILaserToolKernel>();

        void InitializableModule.Init()
        {
            AllTools.TryAdd(MyKernel.Block.EntityId, MyKernel);
        }

        void ClosableModule.Close()
        {

        }

        void UpdatableModule.Update()
        {

        }
    }
}
