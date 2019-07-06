using System.Collections.Generic;
using EemRdx.EntityModules;
using EemRdx.LaserWelders.EntityModules.ProjectorModules;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.LaserWelders
{
    public interface IProjectorKernel : IBlockKernel<ILaserWeldersSessionKernel, IMyProjector>
    {
        IMyProjector Projector { get; }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false)]
    public class ProjectorKernel : LaserWeldersBlockKernelBase<IMyProjector>, IProjectorKernel
    {
        public ProjectorKernel() { }
        public override string DebugKernelName { get; } = nameof(ProjectorKernel);

        public IMyProjector Projector => TypedBlock;

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new ProjectorTermHelperModule(this));
        }
    }
}
