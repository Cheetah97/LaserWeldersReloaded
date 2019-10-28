using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EemRdx;
using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using ListOfBlocks = System.Collections.Generic.IReadOnlyList<VRage.MyTuple<VRage.Game.MyDefinitionId, VRageMath.Vector3I, VRageMath.Vector3D, float, System.Collections.Generic.IReadOnlyDictionary<string, int>>>;

namespace EemRdx.LaserWelders.SessionModules
{
    public class LaserToolTermControlsGeneratorModule : TerminalControlsGeneratorModuleBase<ILaserWeldersSessionKernel, LaserToolKernel, IMyShipWelder>
    {
        public LaserToolTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(LaserToolTermControlsGeneratorModule);

        protected override List<MyTerminalControlBatch> GenerateBatchControls()
        {
            List<MyTerminalControlBatch> Batch = new List<MyTerminalControlBatch>
            {
                ToolMode2(), LaserBeam2(), SpeedMultiplier2(), DistanceMode2(), DumpStone2()
            };
            return Batch;
        }

        protected override List<IMyTerminalControl> GenerateControls()
        {
            return new List<IMyTerminalControl>
            {
                /*ToolMode(), LaserBeam(), SpeedMultiplier(), DistanceMode(), DumpStone(),*/

                MissingComponents(), LastOperatedGrid(), LastOperatedGridBlocks(),
                LastOperatedProjectedGrid(), LastOperatedProjectedGridBlocks()
            };
        }

        protected override List<IMyTerminalAction> GenerateActions()
        {
            return new List<IMyTerminalAction>();
        }

        #region Control Definitions
        private MyTerminalControlBatch LaserBeam2()
        {
            DefaultSliderControlTexts controlTexts;
            controlTexts.Title = "Beam Length";
            controlTexts.Tooltip = "Sets the laser beam's length.";

            DefaultSliderActionTexts actionTexts;
            string textbase = "beam length";
            actionTexts.IncreaseName = $"Increase {textbase}";
            actionTexts.DecreaseName = $"Decrease {textbase}";

            Func<LaserToolKernel, float> getter = kernel => kernel.TermControls.LaserBeamLength;
            Action<LaserToolKernel, float> setter = (kernel, val) => kernel.TermControls.LaserBeamLength = (int)val;
            Func<LaserToolKernel, float> minGetter = kernel => kernel.BeamDrawer.MinBeamLengthBlocks;
            Func<LaserToolKernel, float> maxGetter = kernel => kernel.BeamDrawer.MaxBeamLengthBlocks;
            Func<float, string> writer = blocks => $"{((int)blocks).ToString()} blocks";

            return DefaultSliderControl("BeamLength", controlTexts, actionTexts, SliderLimits.Linear, true, getter, setter, minGetter, maxGetter, 1, writer, InvalidForSeats);
        }

        private MyTerminalControlBatch SpeedMultiplier2()
        {
            DefaultSliderControlTexts controlTexts;
            controlTexts.Title = "Speed Multiplier";
            controlTexts.Tooltip = $"Allows to increase tool's speed at the cost of power usage.\nThis is more CPU-friendly than piling on multiple tools.{(MySessionKernel.Settings.SpeedMultiplierAcceleratesDrilling == false ? "\nDoes not accelerate drilling for performance reasons." : "")}";

            DefaultSliderActionTexts actionTexts;
            string textbase = "speed";
            actionTexts.IncreaseName = $"Increase {textbase}";
            actionTexts.DecreaseName = $"Decrease {textbase}";

            Func<LaserToolKernel, float> getter = kernel => kernel.TermControls.SpeedMultiplier;
            Action<LaserToolKernel, float> setter = (kernel, NewSpeed) => kernel.TermControls.SpeedMultiplier = (int)NewSpeed;
            Func<LaserToolKernel, float> minGetter = kernel => 1;
            Func<LaserToolKernel, float> maxGetter = kernel =>
            {
                string subtypeId = kernel.Block.BlockDefinition.SubtypeName;
                if (MySessionKernel.Settings?.ToolTiers?.ContainsKey(subtypeId) != true) return 1;
                return MySessionKernel.Settings.ToolTiers[subtypeId].SpeedMultiplierMaxValue;
            };
            Func<float, string> writer = multiplier => $"x{((int)multiplier).ToString()}";
            Func<LaserToolKernel, bool> enabled = kernel => maxGetter(kernel) > 1;

            return DefaultSliderControl("SpeedMultiplier", controlTexts, actionTexts, SliderLimits.Linear, true, getter, setter, minGetter, maxGetter, 1, writer, InvalidForSeats, enabled);
        }

        //public IMyTerminalControlSlider SpeedMultiplier()
        //{
        //    IMyTerminalControlSlider SpeedMultiplier = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipWelder>("SpeedMultiplier");
        //    SpeedMultiplier.Title = MyStringId.GetOrCompute("Speed Multiplier");
        //    SpeedMultiplier.Tooltip = MyStringId.GetOrCompute("Allows to increase tool's speed at the cost of power usage.\nThis is more efficient than piling on multiple tools.\nDoes not accelerate drilling for performance reasons.");
        //    SpeedMultiplier.SupportsMultipleBlocks = true;
        //    SpeedMultiplier.Enabled = (Block) => HasBlockLogic(Block) && SpeedMultiplier_MaxGetter(Block) > 1;
        //    SpeedMultiplier.Visible = HasBlockLogic;
        //    SpeedMultiplier.SetLimits(trash => 1, SpeedMultiplier_MaxGetter);
        //    SpeedMultiplier.Getter = (Block) => BlockReturn(Block, x => x.TermControls.SpeedMultiplier);
        //    SpeedMultiplier.Setter = (Block, NewSpeed) => BlockAction(Block, x => x.TermControls.SpeedMultiplier = (int)MathHelper.Clamp(NewSpeed, 1, SpeedMultiplier_MaxGetter(Block)));
        //    SpeedMultiplier.Writer = (Block, Info) => Info.Append($"x{BlockReturn(Block, x => Math.Round(x.TermControls.SpeedMultiplier * WeldGrindSpeedMultiplier_Getter(x.Block), 2))}");
        //    return SpeedMultiplier;
        //}

        float SpeedMultiplier_MaxGetter(IMyTerminalBlock Block)
        {
            string subtypeId = Block.BlockDefinition.SubtypeName;
            if (MySessionKernel.Settings?.ToolTiers?.ContainsKey(subtypeId) != true) return 1;
            return MySessionKernel.Settings.ToolTiers[subtypeId].SpeedMultiplierMaxValue;
        }

        float WeldGrindSpeedMultiplier_Getter(IMyTerminalBlock Block)
        {
            string subtypeId = Block.BlockDefinition.SubtypeName;
            if (MySessionKernel.Settings?.ToolTiers?.ContainsKey(subtypeId) != true) return 1;
            return MySessionKernel.Settings.ToolTiers[subtypeId].WeldGrindSpeedMultiplier;
        }

        private MyTerminalControlBatch DistanceMode2()
        {
            DefaultOnOffControlTexts controlTexts;
            controlTexts.Title = "Single Block Mode";
            controlTexts.Tooltip = "If enabled, the Laser Tool will build furthest block first (if welding) or dismantle closest block first (if grinding) before proceeding on new one.";
            controlTexts.OnText = "Single";
            controlTexts.OffText = "Multi";

            DefaultOnOffToggleActionTexts actionTexts;
            string textbase = "Single Block mode";
            actionTexts.ToggleTitle = $"Toggle {textbase}";
            actionTexts.SwitchOnTitle = $"Enable {textbase}";
            actionTexts.SwitchOffTitle = $"Disable {textbase}";
            actionTexts.ToggledOnText = "Single";
            actionTexts.ToggledOffText = "Multi";

            Func<LaserToolKernel, bool> getter = kernel => kernel.TermControls.DistanceMode;
            Action<LaserToolKernel, bool> setter = (kernel, NewMode) => kernel.TermControls.DistanceMode = NewMode;
            return DefaultOnOffSwitch("DistanceMode", controlTexts, actionTexts, true, getter, setter, InvalidForSeats);
        }

        //public IMyTerminalControlCheckbox DistanceMode()
        //{
        //    IMyTerminalControlCheckbox DistanceMode = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipWelder>("DistanceMode");
        //    DistanceMode.SupportsMultipleBlocks = true;
        //    DistanceMode.Enabled = HasBlockLogic;
        //    DistanceMode.Visible = HasBlockLogic;
        //    DistanceMode.Getter = (Block) => BlockReturn(Block, x => x.TermControls.DistanceMode);
        //    DistanceMode.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.DistanceMode = NewMode);
        //    DistanceMode.Title = MyStringId.GetOrCompute("Single Block Mode");
        //    DistanceMode.Tooltip = MyStringId.GetOrCompute($"If enabled, the Laser Tool will build furthest block first (if welding) or dismantle closest block first (if grinding) before proceeding on new one.");
        //    return DistanceMode;
        //}

        private MyTerminalControlBatch ToolMode2()
        {
            DefaultOnOffControlTexts controlTexts;
            controlTexts.Title = "Tool Mode";
            controlTexts.Tooltip = $"Switches between Laser Welder and Laser Grinder modes.{(MySessionKernel.Settings.EnableDrilling ? "\r\nIn Grinder mode, the tool can also mine voxels." : "")}";
            controlTexts.OnText = "WELD";
            controlTexts.OffText = "GRIND";

            DefaultOnOffToggleActionTexts actionTexts;
            actionTexts.ToggleTitle = "Toggle Laser Tool mode";
            actionTexts.SwitchOnTitle = "Enable Welder mode";
            actionTexts.SwitchOffTitle = "Enable Grinder mode";
            actionTexts.ToggledOnText = "Welder";
            actionTexts.ToggledOffText = "Grinder";

            Func<LaserToolKernel, bool> getter = kernel => kernel.TermControls.ToolMode;
            Action<LaserToolKernel, bool> setter = (kernel, NewMode) => kernel.TermControls.ToolMode = NewMode;
            return DefaultOnOffSwitch("ToolMode", controlTexts, actionTexts, true, getter, setter, InvalidForSeats);
        }

        //public IMyTerminalControlOnOffSwitch ToolMode()
        //{
        //    IMyTerminalControlOnOffSwitch ToolMode = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyShipWelder>("ToolMode");
        //    ToolMode.SupportsMultipleBlocks = true;
        //    ToolMode.Enabled = HasBlockLogic;
        //    ToolMode.Visible = HasBlockLogic;
        //    ToolMode.Getter = (Block) => BlockReturn(Block, x => x.TermControls.ToolMode);
        //    ToolMode.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.ToolMode = NewMode);
        //    ToolMode.Title = MyStringId.GetOrCompute("Tool Mode");
        //    ToolMode.Tooltip = MyStringId.GetOrCompute($"Switches between Laser Welder and Laser Grinder modes.{(MySessionKernel.Settings.EnableDrilling ? "\r\nIn Grinder mode, the tool can also mine voxels." : "")}");
        //    ToolMode.OnText = MyStringId.GetOrCompute("WELD");
        //    ToolMode.OffText = MyStringId.GetOrCompute("GRIND");

        //    return ToolMode;
        //}

        private MyTerminalControlBatch DumpStone2()
        {
            DefaultOnOffControlTexts controlTexts;
            controlTexts.Title = "Dump Stone";
            controlTexts.Tooltip = "If enabled, the Laser Tool will not pick up stone when drilling voxels.";
            controlTexts.OnText = "Dump";
            controlTexts.OffText = "Pick";

            DefaultOnOffToggleActionTexts actionTexts;
            string textbase = "dumping stone";
            actionTexts.ToggleTitle = $"Toggle {textbase}";
            actionTexts.SwitchOnTitle = $"Enable {textbase}";
            actionTexts.SwitchOffTitle = $"Disable {textbase}";
            actionTexts.ToggledOnText = "Dump";
            actionTexts.ToggledOffText = "Pick";

            Func<LaserToolKernel, bool> getter = kernel => kernel.TermControls.DumpStone;
            Action<LaserToolKernel, bool> setter = (kernel, NewMode) => kernel.TermControls.DumpStone = NewMode;
            Func<LaserToolKernel, bool> enabled = kernel => MySessionKernel.Settings.EnableDrilling;
            return DefaultOnOffSwitch("DumpStone", controlTexts, actionTexts, true, getter, setter, InvalidForSeats, enabled);
        }
        #endregion

        #region Property Definitions
        public IMyTerminalControlProperty<IReadOnlyDictionary<MyItemType, float>> MissingComponents()
        {
            IMyTerminalControlProperty<IReadOnlyDictionary<MyItemType, float>> MissingComponents = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyDictionary<MyItemType, float>, IMyShipWelder>("MissingComponents");
            MissingComponents.Enabled = Block => HasBlockLogic(Block);
            MissingComponents.Getter = MissingComponents_Getter;
            MissingComponents.Setter = DefaultInvalidPropertySetter;

            return MissingComponents;
        }

        private IReadOnlyDictionary<MyItemType, float> MissingComponents_Getter(IMyTerminalBlock Block)
        {
            return BlockReturn(Block, kernel => kernel.Responder.LastReportedMissingComponents);
        }

        private IMyTerminalControlProperty<IReadOnlyList<long>> LastOperatedGrid()
        {
            IMyTerminalControlProperty<IReadOnlyList<long>> LastOperatedGrid = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyList<long>, IMyShipWelder>("TargetGrids");
            LastOperatedGrid.Enabled = Block => HasBlockLogic(Block);
            LastOperatedGrid.Getter = LastOperatedGrid_Getter;
            LastOperatedGrid.Setter = DefaultInvalidPropertySetter;

            return LastOperatedGrid;
        }

        private IReadOnlyList<long> LastOperatedGrid_Getter(IMyTerminalBlock Block)
        {
            return BlockReturn(Block, kernel => kernel.Responder.LastOperatedGrids.Select(x => x.EntityId).ToList().AsReadOnly());
        }

        private IMyTerminalControlProperty<IReadOnlyList<ListOfBlocks>> LastOperatedGridBlocks()
        {
            IMyTerminalControlProperty<IReadOnlyList<ListOfBlocks>> LastOperatedGridBlocks = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyList<ListOfBlocks>, IMyShipWelder>("TargetGridsBlocks");
            LastOperatedGridBlocks.Enabled = Block => HasBlockLogic(Block);
            LastOperatedGridBlocks.Getter = LastOperatedGridBlocks_Getter;
            LastOperatedGridBlocks.Setter = DefaultInvalidPropertySetter;

            return LastOperatedGridBlocks;
        }

        private IReadOnlyList<ListOfBlocks> LastOperatedGridBlocks_Getter(IMyTerminalBlock Block)
        {
            var Retval = new List<ListOfBlocks>(0);

            LaserToolKernel LaserKernel;
            if (!Block.TryGetComponent(out LaserKernel)) return null;

            foreach (IMyCubeGrid grid in LaserKernel.Responder.LastOperatedGrids)
            {
                GridKernel gridKernel;
                if (grid.TryGetComponent(out gridKernel)) Retval.Add(gridKernel.BlockDataCachingModule.BlockDataCache);
            }

            return Retval;
        }

        private IMyTerminalControlProperty<IReadOnlyList<long>> LastOperatedProjectedGrid()
        {
            IMyTerminalControlProperty<IReadOnlyList<long>> LastOperatedProjectedGrid = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyList<long>, IMyShipWelder>("TargetProjectedGrids");
            LastOperatedProjectedGrid.Enabled = Block => HasBlockLogic(Block);
            LastOperatedProjectedGrid.Getter = LastOperatedProjectedGrid_Getter;
            LastOperatedProjectedGrid.Setter = DefaultInvalidPropertySetter;

            return LastOperatedProjectedGrid;
        }

        private IReadOnlyList<long> LastOperatedProjectedGrid_Getter(IMyTerminalBlock Block)
        {
            return BlockReturn(Block, kernel => kernel.Responder.LastOperatedProjectedGrids.Select(x => x.EntityId).ToList().AsReadOnly());
        }

        private IMyTerminalControlProperty<IReadOnlyList<ListOfBlocks>> LastOperatedProjectedGridBlocks()
        {
            IMyTerminalControlProperty<IReadOnlyList<ListOfBlocks>> LastOperatedProjectedGridBlocks = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyList<ListOfBlocks>, IMyShipWelder>("TargetProjectedGridsBlocks");
            LastOperatedProjectedGridBlocks.Enabled = Block => HasBlockLogic(Block);
            LastOperatedProjectedGridBlocks.Getter = LastOperatedProjectedGridBlocks_Getter;
            LastOperatedProjectedGridBlocks.Setter = DefaultInvalidPropertySetter;

            return LastOperatedProjectedGridBlocks;
        }

        private IReadOnlyList<ListOfBlocks> LastOperatedProjectedGridBlocks_Getter(IMyTerminalBlock Block)
        {
            var Retval = new List<ListOfBlocks>(0);

            LaserToolKernel LaserKernel;
            if (!Block.TryGetComponent(out LaserKernel)) return null;

            foreach (IMyCubeGrid grid in LaserKernel.Responder.LastOperatedGrids)
            {
                GridKernel gridKernel;
                if (grid.TryGetComponent(out gridKernel)) Retval.Add(gridKernel.BlockDataCachingModule.BlockDataCache);
            }

            return Retval;
        }
        #endregion
    }
}
