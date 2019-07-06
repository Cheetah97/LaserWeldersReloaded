using System.Collections.Generic;
using EemRdx.EntityModules;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.LaserWelders
{
    public interface IJumpKernel : IBlockKernel<ILaserWeldersSessionKernel, IMyJumpDrive>
    {
        IMyJumpDrive JumpDrive { get; }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_JumpDrive), false)]
    public class JumpKernel : LaserWeldersBlockKernelBase<IMyJumpDrive>, IJumpKernel
    {
        public JumpKernel() { }

        public override string DebugKernelName { get; } = nameof(JumpKernel);

        public IMyJumpDrive JumpDrive => TypedBlock;

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new JumpTermHelper(this));
        }
    }

    public class JumpTermHelper : TerminalControlsHelperModuleBase<IJumpKernel, SessionModules.JumpTerminalControlsGenerator>
    {
        public JumpTermHelper(IJumpKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(JumpTermHelper);
    }
}
