using System.Collections.Generic;
using EemRdx.EntityModules;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.LaserWelders
{
    public class BlockLimitsHelper : EntityModuleBase<IGridBlockLimitKernel>, InitializableModule, UpdatableModule, ClosableModule
    {
        public BlockLimitsHelper(IGridBlockLimitKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(BlockLimitsHelper);

        public bool Inited { get; private set; }

        bool UpdatableModule.RequiresOperable { get; } = false;

        MyEntityUpdateEnum UpdatableModule.UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_10TH_FRAME;

        private List<IMySlimBlock> RecentlyAddedBlocks = new List<IMySlimBlock>();
        private List<IMySlimBlock> RecentlyRemovedBlocks = new List<IMySlimBlock>();
        void InitializableModule.Init()
        {
            if (Inited) return;
            MyKernel.Grid.OnBlockAdded += Grid_OnBlockAdded;
            MyKernel.Grid.OnBlockRemoved += Grid_OnBlockRemoved;
            Inited = true;
        }

        private void Grid_OnBlockAdded(IMySlimBlock block)
        {
            lock (RecentlyAddedBlocks)
            {
                RecentlyAddedBlocks.Add(block);
            }
        }

        private void Grid_OnBlockRemoved(IMySlimBlock block)
        {
            lock (RecentlyRemovedBlocks)
            {
                RecentlyRemovedBlocks.Add(block);
            }
        }

        void UpdatableModule.Update()
        {
            foreach (IMySlimBlock block in RecentlyRemovedBlocks)
            {
                MyKernel.Session.BlockLimits.RemoveBlock(block);
            }
            RecentlyRemovedBlocks.Clear();

            foreach (IMySlimBlock block in RecentlyAddedBlocks)
            {
                MyKernel.Session.BlockLimits.AddBlock(block);
            }
            RecentlyAddedBlocks.Clear();
        }

        void ClosableModule.Close()
        {
            MyKernel.Grid.OnBlockAdded -= Grid_OnBlockAdded;
            MyKernel.Grid.OnBlockRemoved -= Grid_OnBlockRemoved;
        }
    }

    public interface IGridBlockLimitKernel : IEntityKernel
    {
        ILaserWeldersSessionKernel Session { get; }
        IMyCubeGrid Grid { get; }
    }

    public class GridBlockLimitKernel : EntityKernel, IGridBlockLimitKernel
    {
        public override string DebugKernelName { get; } = nameof(GridBlockLimitKernel);

        public ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
        public IMyCubeGrid Grid => Entity as IMyCubeGrid;

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new BlockLimitsHelper(this));
        }

        public GridBlockLimitKernel() { }
    }
}
