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
        IBlockDataCachingModule BlockDataCachingModule { get; }
        IInventorySystem InventorySystem { get; }
        IMultigridder Multigridder { get; }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid), false)]
    public class GridKernel : EntityKernel<ILaserWeldersSessionKernel>, IGridKernel
    {
        public override string DebugKernelName { get; } = nameof(GridKernel);

        public override ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
        public IMyCubeGrid Grid => Entity as IMyCubeGrid;
        public IWeaponsFireDetectionModule WeaponsFireDetection => GetModule<IWeaponsFireDetectionModule>();
        public IBlockDataCachingModule BlockDataCachingModule => GetModule<IBlockDataCachingModule>();
        public IInventorySystem InventorySystem => GetModule<IInventorySystem>();
        public IMultigridder Multigridder => GetModule<IMultigridder>();

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new MultigridderModule(this));
            EntityModules.Add(new WeaponsFireDetectionModule(this));
            EntityModules.Add(new BlockDataCachingModule(this));
            EntityModules.Add(new InventorySystemModule(this));
            //EntityModules.Add(new BlockLimitsHelper(this));
        }

        public GridKernel() { }
    }
}
