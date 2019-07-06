using System;
using EemRdx;
using EemRdx.EntityModules;
using EemRdx.LaserWelders.SessionModules;
using EemRdx.LaserWelders.EntityModules;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.Components;
using EemRdx.LaserWelders.EntityModules.PyroboltModules;

namespace EemRdx.LaserWelders
{
    public class PyroboltTermGeneratorModule : TerminalControlsHelperModuleBase<IPyroboltKernel, PyroboltTermControlsGeneratorModule>
    {
        public PyroboltTermGeneratorModule(IPyroboltKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(PyroboltTermGeneratorModule);
    }

    public interface IPyroboltKernel : IBlockKernel<ILaserWeldersSessionKernel, IMyConveyorSorter>
    {
        IMyConveyorSorter Sorter { get; }
        IPyroboltModule PyroboltModule { get; }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "PyroboltSmall", "PyroboltMedium", "PyroboltLarge")]
    public class PyroboltKernel : LaserWeldersBlockKernelBase<IMyConveyorSorter>, IPyroboltKernel
    {
        public PyroboltKernel() { }

        public override string DebugKernelName { get; } = nameof(PyroboltKernel);
        public IMyConveyorSorter Sorter => TypedBlock;
        public IPyroboltModule PyroboltModule => GetModule<IPyroboltModule>();

        protected override void CreateModules()
        {
            base.CreateModules();
            EntityModules.Add(new PyroboltTermGeneratorModule(this));
            EntityModules.Add(new PyroboltModule(this));
        }
    }
}
