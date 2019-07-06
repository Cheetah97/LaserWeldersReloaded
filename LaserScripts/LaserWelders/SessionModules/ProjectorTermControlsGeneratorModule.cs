using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace EemRdx.LaserWelders.SessionModules
{
    public class ProjectorTermControlsGeneratorModule : TerminalControlsGeneratorModuleBase<ILaserWeldersSessionKernel, ProjectorKernel, IMyProjector>
    {
        public ProjectorTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(ProjectorTermControlsGeneratorModule);

        protected override List<MyTerminalControlBatch> GenerateBatchControls()
        {
            return new List<MyTerminalControlBatch>();
        }

        protected override List<IMyTerminalAction> GenerateActions()
        {
            return new List<IMyTerminalAction>();
        }

        protected override List<IMyTerminalControl> GenerateControls()
        {
            return new List<IMyTerminalControl>
            {
                ProjectedGridBlocks()
            };
        }

        private IMyTerminalControlProperty<IReadOnlyList<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>>> ProjectedGridBlocks()
        {
            IMyTerminalControlProperty<IReadOnlyList<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>>> ProjectedGridBlocks = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyList<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>>, IMyProjector>("ProjectedGridBlocks");
            ProjectedGridBlocks.Enabled = Block => HasBlockLogic(Block);
            ProjectedGridBlocks.Getter = ProjectedGridBlocks_Getter;
            ProjectedGridBlocks.Setter = DefaultInvalidPropertySetter;

            return ProjectedGridBlocks;
        }

        private IReadOnlyList<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>> ProjectedGridBlocks_Getter(IMyTerminalBlock Block)
        {
            var DefaultRetval = new List<MyTuple<MyDefinitionId, Vector3I, Vector3D, float, IReadOnlyDictionary<string, int>>>(0);

            ProjectorKernel projectorKernel;
            if (!Block.TryGetComponent(out projectorKernel)) return null;

            IMyCubeGrid grid = projectorKernel.Projector.ProjectedGrid;
            if (grid == null) return DefaultRetval;

            GridKernel gridKernel;
            if (!grid.TryGetComponent(out gridKernel)) return DefaultRetval;

            return gridKernel.BlockDataCachingModule.BlockDataCache;
        }
    }
}
