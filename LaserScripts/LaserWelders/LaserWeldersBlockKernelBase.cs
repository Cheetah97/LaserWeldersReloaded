using System;
using EemRdx;
using EemRdx.EntityModules;
using EemRdx.LaserWelders;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace EemRdx.LaserWelders
{
    public abstract class LaserWeldersBlockKernelBase<TBlock> : BlockKernelBase<ILaserWeldersSessionKernel, TBlock> where TBlock: IMyTerminalBlock
    {
        public override ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
    }

    public abstract class LaserWeldersTerminalBlockKernelBase<TBlock, TPersistentStruct, TPersistenceModule, TTermControlsModule> : TerminalBlockKernelBase<ILaserWeldersSessionKernel, TBlock, TPersistentStruct, TPersistenceModule, TTermControlsModule> where TBlock : IMyTerminalBlock where TPersistentStruct : struct, IPersistentStruct where TPersistenceModule : IPersistence where TTermControlsModule : ITerminalControls
    {
        public override ILaserWeldersSessionKernel Session => LaserWeldersSessionKernel.LaserWeldersSession;
    }
}
