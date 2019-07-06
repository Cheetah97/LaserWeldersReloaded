using System;
using System.Collections.Generic;
using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace EemRdx.LaserWelders.SessionModules
{
    public class PyroboltTermControlsGeneratorModule : TerminalControlsGeneratorModuleBase<ILaserWeldersSessionKernel, PyroboltKernel, IMyConveyorSorter>
    {
        public PyroboltTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(PyroboltTermControlsGeneratorModule);

        protected override List<MyTerminalControlBatch> GenerateBatchControls()
        {
            return new List<MyTerminalControlBatch>();
        }

        protected override List<IMyTerminalControl> GenerateControls()
        {
            return new List<IMyTerminalControl>
            {
                DetonateButton()
            };
        }

        protected override List<IMyTerminalAction> GenerateActions()
        {
            return new List<IMyTerminalAction>
            {
                DetonateAction()
            };
        }

        public IMyTerminalControlButton DetonateButton()
        {
            IMyTerminalControlButton button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyConveyorSorter>("DetonateButton");
            button.Enabled = HasBlockLogic;
            button.Visible = HasBlockLogic;
            button.SupportsMultipleBlocks = true;
            button.Action = block => BlockAction(block, kernel => kernel.PyroboltModule.TryDetonate());
            button.Title = MyStringId.GetOrCompute("Detonate");
            button.Tooltip = MyStringId.GetOrCompute("Safely detonates the pyrobolt without harm to surrounding blocks, and separates two grids.");
            return button;
        }
        public IMyTerminalAction DetonateAction()
        {
            IMyTerminalAction button = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>("Detonate");
            button.Enabled = HasBlockLogic;
            button.ValidForGroups = true;
            button.Action = block => BlockAction(block, kernel => kernel.PyroboltModule.TryDetonate());
            button.Name = new System.Text.StringBuilder("Detonate");
            return button;
        }
    }
}
