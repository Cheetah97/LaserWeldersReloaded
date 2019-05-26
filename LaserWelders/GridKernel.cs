using System.Collections.Generic;
using EemRdx.EntityModules;
using EemRdx.LaserWelders.EntityModules.GridModules;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.LaserWelders
{
    public interface IGridKernel : IEntityKernel
    {
        ILaserWeldersSessionKernel Session { get; }
        IMyCubeGrid Grid { get; }
        IWeaponsFireDetectionModule WeaponsFireDetection { get; }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid), false)]
    public class GridKernel : EntityKernel, IGridKernel
    {
        public override string DebugKernelName { get; } = nameof(GridKernel);

        public ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
        public IMyCubeGrid Grid => Entity as IMyCubeGrid;
        public IWeaponsFireDetectionModule WeaponsFireDetection => GetModule<IWeaponsFireDetectionModule>();

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new WeaponsFireDetectionModule(this));
            EntityModules.Add(new BlockLimitsHelper(this));
        }

        public GridKernel() { }
    }
}
