using EemRdx.EntityModules;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace EemRdx.LaserWelders.EntityModules.GridModules
{
    public interface IBlockDataCachingModule : IEntityModule
    {
        IReadOnlyList<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>> BlockDataCache { get; }
    }

    public class BlockDataCachingModule : EntityModuleBase<IGridKernel>, UpdatableModule, IBlockDataCachingModule
    {
        public BlockDataCachingModule(IGridKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(BlockDataCachingModule);

        public IReadOnlyList<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>> BlockDataCache
        {
            get
            {
                if (BlockDataCacheNeedsRefresh) UpdateCache();
                return _BlockDataCache.AsReadOnly();
            }
        }

        private readonly TimeSpan UpdateFrequency = TimeSpan.FromSeconds(2);
        private bool BlockDataCacheNeedsRefresh => (DateTime.UtcNow - LastCacheUpdate) >= UpdateFrequency;

        bool UpdatableModule.RequiresOperable { get; } = false;

        MyEntityUpdateEnum UpdatableModule.UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_100TH_FRAME;

        private DateTime LastCacheUpdate;
        private readonly List<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>> _BlockDataCache = new List<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>>();

        private readonly List<IMySlimBlock> ReusableBlocksList = new List<IMySlimBlock>();
        private void UpdateCache()
        {
            LastCacheUpdate = DateTime.UtcNow;
            int CurrentBlockCount = (MyKernel.Grid as Sandbox.Game.Entities.MyCubeGrid).BlocksCount;
            ReusableBlocksList.Clear();
            ReusableBlocksList.Capacity = CurrentBlockCount;
            _BlockDataCache.Clear();
            _BlockDataCache.Capacity = CurrentBlockCount;
            MyKernel.Grid.GetBlocks(ReusableBlocksList);

            foreach (IMySlimBlock block in ReusableBlocksList) _BlockDataCache.Add(BlockDataToTuple(block));
        }

        private MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>> BlockDataToTuple(IMySlimBlock Block)
        {
            MyDefinitionId id = Block.BlockDefinition.Id;
            Vector3I gridPos = Block.Position;
            Vector3D worldPos = Block.CubeGrid.GridIntegerToWorld(Block.Position);
            float integrityRatio = (Block.BuildIntegrity - Block.CurrentDamage) / Block.MaxIntegrity;
            Dictionary<string, int> missingComponents = new Dictionary<string, int>();
            if (integrityRatio < 1 && Block.StockpileAllocated) Block.GetMissingComponents(missingComponents);

            return MyTuple.Create<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>(id, gridPos, worldPos, integrityRatio, missingComponents);
        }

        void UpdatableModule.Update()
        {
            if ((DateTime.UtcNow - LastCacheUpdate) >= (UpdateFrequency + UpdateFrequency)) ClearCache();
        }

        private void ClearCache()
        {
            _BlockDataCache.Clear();
        }
    }
}
