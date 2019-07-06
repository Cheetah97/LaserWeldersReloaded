using EemRdx.EntityModules;
using EemRdx.Extensions;
using EemRdx.LaserWelders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace EemRdx.LaserWelders.EntityModules.GridModules
{
    public interface IMultigridder : IEntityModule
    {
        IReadOnlyList<IGridKernel> CompleteGrid { get; }
        IGridKernel BiggestGrid { get; }
    }

    public class MultigridderModule : EntityModuleBase<IGridKernel>, InitializableModule, UpdatableModule, IMultigridder
    {
        public MultigridderModule(IGridKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(MultigridderModule);
        public bool Inited { get; private set; }
        bool UpdatableModule.RequiresOperable { get; } = false;
        MyEntityUpdateEnum UpdatableModule.UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;

        public IReadOnlyList<IGridKernel> CompleteGrid => _CompleteGrid.AsReadOnly();
        public IGridKernel BiggestGrid { get; private set; }
        private List<IGridKernel> _CompleteGrid = new List<IGridKernel>(10);
        private int Ticker => MyKernel.Session.Clock.Ticker;

        void InitializableModule.Init()
        {
            if (Inited) return;
            BiggestGrid = MyKernel;
            UpdateMultigrid();
            Inited = true;
        }

        void UpdatableModule.Update()
        {
            if (Ticker % 60 == 0) UpdateMultigrid();
        }

        private void UpdateMultigrid()
        {
            _CompleteGrid.Clear();
            List<IMyCubeGrid> grids = MyAPIGateway.GridGroups.GetGroup(MyKernel.Grid, GridLinkTypeEnum.Logical);
            if (_CompleteGrid.Capacity < grids.Count + 1) _CompleteGrid.Capacity = grids.Count + 1;

            foreach (IMyCubeGrid Grid in grids)
            {
                GridKernel Kernel;
                if (Grid.TryGetComponent(out Kernel)) _CompleteGrid.Add(Kernel);
            }

            BiggestGrid = _CompleteGrid.OrderByDescending(x => (x.Grid as Sandbox.Game.Entities.MyCubeGrid).BlocksCount).First();
        }
    }
}
