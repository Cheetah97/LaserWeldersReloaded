using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Utils;
using EemRdx.EntityModules;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using System.Collections.Generic;
using EemRdx.Extensions;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public interface IToolListProvider : IEntityModule
    {
        IReadOnlyList<ILaserToolKernel> Tools { get; }
    }

    public class ToolListProvider : EntityModuleBase<ISCKernel>, InitializableModule, UpdatableModule, IToolListProvider
    {
        public ToolListProvider(ISCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(ToolListProvider);

        public bool Inited { get; } = false;
        public bool RequiresOperable { get; } = false;
        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_10TH_FRAME;

        private IMyGridTerminalSystem Term;
        private int Ticker => MyKernel.Session.Clock.Ticker;
        IReadOnlyList<ILaserToolKernel> IToolListProvider.Tools => Tools;
        private List<ILaserToolKernel> Tools = new List<ILaserToolKernel>();

        void InitializableModule.Init()
        {
            Term = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(MyKernel.Block.CubeGrid);
        }

        void UpdatableModule.Update()
        {
            if (Ticker % 3 * 60 == 0)
                UpdateToolList();
        }

        private void UpdateToolList()
        {
            Tools.Clear();
            List<IMyShipGrinder> grinders = new List<IMyShipGrinder>();
            Term.GetBlocksOfType(grinders);
            foreach (IMyShipGrinder grinder in grinders)
            {
                LaserToolKernel kernel;
                if (grinder.TryGetComponent(out kernel))
                {
                    Tools.Add(kernel);
                }
            }
        }
    }
}
