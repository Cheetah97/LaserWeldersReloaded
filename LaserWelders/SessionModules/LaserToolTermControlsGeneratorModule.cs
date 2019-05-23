using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EemRdx;
using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface ILaserToolTermControlsGenerator : ISessionModule
    {
        void EnsureToolControls();
    }

    public class LaserToolTermControlsGeneratorModule : SessionModuleBase<ILaserWeldersSessionKernel>, ILaserToolTermControlsGenerator
    {
        public LaserToolTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(LaserToolTermControlsGeneratorModule);

        private bool ToolControlsInited = false;

        public void EnsureToolControls()
        {
            if (ToolControlsInited) return;
            ToolControlsInited = true;

            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(ToolMode());
            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(LaserBeam());
            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(SpeedMultiplier());
            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(DistanceMode());
            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(DumpStone());

            MyAPIGateway.TerminalControls.AddAction<IMyShipGrinder>(ToggleToolMode());
            MyAPIGateway.TerminalControls.AddAction<IMyShipGrinder>(ToggleToolMode_Welder());
            MyAPIGateway.TerminalControls.AddAction<IMyShipGrinder>(ToggleToolMode_Grinder());

            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(MissingComponents());
        }

        #region Term-to-Block Interfacing
        public bool HasBlockLogic(IMyTerminalBlock Block)
        {
            try
            {
                return Block.HasComponent<LaserToolKernel>();
            }
            catch (Exception Scrap)
            {
                MySessionKernel.Log?.GeneralLog?.LogError($"{DebugModuleName}.HasBlockLogic", "HasComponent threw", Scrap);
                return false;
            }
        }

        public void BlockAction(IMyTerminalBlock Block, Action<LaserToolKernel> Action)
        {
            try
            {
                LaserToolKernel Logic;
                if (!Block.TryGetComponent(out Logic)) return;
                Action(Logic);
            }
            catch (Exception Scrap)
            {
                MySessionKernel.Log?.GeneralLog?.LogError($"{DebugModuleName}.BlockAction", "BlockAction threw", Scrap);
                return;
            }
        }

        public T BlockReturn<T>(IMyTerminalBlock Block, Func<LaserToolKernel, T> Getter, T Default = default(T))
        {
            try
            {
                LaserToolKernel Logic;
                if (!Block.TryGetComponent(out Logic)) return Default;
                return Getter(Logic);
            }
            catch (Exception Scrap)
            {
                MySessionKernel.Log?.GeneralLog?.LogError($"{DebugModuleName}.BlockReturn", "BlockReturn threw", Scrap);
                return Default;
            }
        }
        #endregion

        #region Control Definitions
        public IMyTerminalControlSlider LaserBeam()
        {
            IMyTerminalControlSlider LaserBeam = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipGrinder>("BeamLength");
            LaserBeam.Title = MyStringId.GetOrCompute("Beam Length");
            LaserBeam.Tooltip = MyStringId.GetOrCompute("Sets the laser beam's length.");
            LaserBeam.SupportsMultipleBlocks = true;
            LaserBeam.Enabled = HasBlockLogic;
            LaserBeam.Visible = HasBlockLogic;
            LaserBeam.SetLimits(LaserBeamLength_MinGetter, LaserBeamLength_MaxGetter);
            LaserBeam.Getter = LaserBeamLength_Getter;
            LaserBeam.Setter = LaserBeamLength_Setter;
            LaserBeam.Writer = LaserBeamLength_Writer;
            return LaserBeam;
        }

        float LaserBeamLength_MinGetter(IMyTerminalBlock Block)
        {
            return BlockReturn(Block, x => x.BeamDrawer.MinBeamLengthBlocks);
        }

        float LaserBeamLength_MaxGetter(IMyTerminalBlock Block)
        {
            return BlockReturn(Block, x => x.BeamDrawer.MaxBeamLengthBlocks);
        }

        float LaserBeamLength_Getter(IMyTerminalBlock Block)
        {
            return BlockReturn(Block, x => x.TermControls.LaserBeamLength);
        }

        void LaserBeamLength_Setter(IMyTerminalBlock Block, float NewLength)
        {
            BlockAction(Block, x => x.TermControls.LaserBeamLength = (int)NewLength);
        }

        void LaserBeamLength_Writer(IMyTerminalBlock Block, StringBuilder Info)
        {
            Info.Append($"{BlockReturn(Block, x => x.TermControls.LaserBeamLength)} blocks");
        }

        public IMyTerminalControlSlider SpeedMultiplier()
        {
            IMyTerminalControlSlider SpeedMultiplier = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipGrinder>("SpeedMultiplier");
            SpeedMultiplier.Title = MyStringId.GetOrCompute("Speed Multiplier");
            SpeedMultiplier.Tooltip = MyStringId.GetOrCompute("Allows to increase tool's speed at the cost of power usage.\nThis is more efficient than piling on multiple tools.\nDoes not accelerate drilling for performance reasons.");
            SpeedMultiplier.SupportsMultipleBlocks = true;
            SpeedMultiplier.Enabled = HasBlockLogic;
            SpeedMultiplier.Visible = HasBlockLogic;
            SpeedMultiplier.SetLimits(trash => 1, SpeedMultiplier_MaxGetter);
            SpeedMultiplier.Getter = (Block) => BlockReturn(Block, x => x.TermControls.SpeedMultiplier);
            SpeedMultiplier.Setter = (Block, NewSpeed) => BlockAction(Block, x => x.TermControls.SpeedMultiplier = (int)NewSpeed);
            SpeedMultiplier.Writer = (Block, Info) => Info.Append($"x{BlockReturn(Block, x => Math.Round(x.TermControls.SpeedMultiplier * WeldGrindSpeedMultiplier_Getter(x.Block), 2))}");
            return SpeedMultiplier;
        }

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

        public IMyTerminalControlCheckbox DistanceMode()
        {
            IMyTerminalControlCheckbox DistanceMode = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipGrinder>("DistanceMode");
            DistanceMode.SupportsMultipleBlocks = true;
            DistanceMode.Enabled = HasBlockLogic;
            DistanceMode.Visible = HasBlockLogic;
            DistanceMode.Getter = (Block) => BlockReturn(Block, x => x.TermControls.DistanceMode);
            DistanceMode.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.DistanceMode = NewMode);
            DistanceMode.Title = MyStringId.GetOrCompute("Distance-based Mode");
            DistanceMode.Tooltip = MyStringId.GetOrCompute($"If enabled, the Laser Tool will build furthest block first (if welding) or dismantle closest block first (if grinding) before proceeding on new one.");
            return DistanceMode;
        }

        public IMyTerminalControlOnOffSwitch ToolMode()
        {
            IMyTerminalControlOnOffSwitch ToolMode = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyShipGrinder>("ToolMode");
            ToolMode.SupportsMultipleBlocks = true;
            ToolMode.Enabled = HasBlockLogic;
            ToolMode.Visible = HasBlockLogic;
            ToolMode.Getter = (Block) => BlockReturn(Block, x => x.TermControls.ToolMode);
            ToolMode.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.ToolMode = NewMode);
            ToolMode.Title = MyStringId.GetOrCompute("Tool Mode");
            ToolMode.Tooltip = MyStringId.GetOrCompute($"Switches between Laser Welder and Laser Grinder modes.{(MySessionKernel.Settings.EnableDrilling ? "\r\nIn Grinder mode, the tool can also mine voxels." : "")}");
            ToolMode.OnText = MyStringId.GetOrCompute("WELD");
            ToolMode.OffText = MyStringId.GetOrCompute("GRIND");

            return ToolMode;
        }

        public IMyTerminalControlCheckbox DumpStone()
        {
            IMyTerminalControlCheckbox DumpStone = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipGrinder>("DumpStone");
            DumpStone.SupportsMultipleBlocks = true;
            DumpStone.Enabled = (Block) => MySessionKernel.Settings.EnableDrilling && HasBlockLogic(Block);
            DumpStone.Visible = HasBlockLogic;
            DumpStone.Getter = (Block) => BlockReturn(Block, x => x.TermControls.DumpStone);
            DumpStone.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.DumpStone = NewMode);
            DumpStone.Title = MyStringId.GetOrCompute("Dump Stone");
            DumpStone.Tooltip = MyStringId.GetOrCompute($"If enabled, the Laser Tool will not pick up stone when drilling voxels.");
            return DumpStone;
        }
        #endregion

        #region Action Definitions
        public IMyTerminalAction ToggleToolMode()
        {
            IMyTerminalAction ToggleToolMode = MyAPIGateway.TerminalControls.CreateAction<IMyShipGrinder>("ToggleToolMode");
            ToggleToolMode.Enabled = Block => HasBlockLogic(Block);
            ToggleToolMode.Name = new StringBuilder("Toggle Laser Tool Mode");
            //ToggleToolMode.Writer = ToggleToolMode_IconWriter;
            ToggleToolMode.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            ToggleToolMode.InvalidToolbarTypes = new List<VRage.Game.MyToolbarType> { VRage.Game.MyToolbarType.Seat };
            ToggleToolMode.ValidForGroups = true;
            ToggleToolMode.Action = Block => BlockAction(Block, x => x.TermControls.ToolMode = !x.TermControls.ToolMode);
            return ToggleToolMode;
        }

        //void ToggleToolMode_IconWriter(IMyTerminalBlock Block, StringBuilder info)
        //{
        //    if (!HasBlockLogic(Block)) return;
        //    info.Clear();
        //    bool ToolMode = BlockReturn(Block, kernel => kernel.TermControls.ToolMode);
        //    if (ToolMode = EntityModules.LaserTerminalControlsModule.ToolModeWelder)
        //        info.Append("WELD");
        //    else if (ToolMode = EntityModules.LaserTerminalControlsModule.ToolModeGrinder)
        //        info.Append("GRIND");
        //}

        public IMyTerminalAction ToggleToolMode_Welder()
        {
            IMyTerminalAction ToggleToolMode_Welder = MyAPIGateway.TerminalControls.CreateAction<IMyShipGrinder>("ToggleToolMode_Welder");
            ToggleToolMode_Welder.Enabled = Block => HasBlockLogic(Block);
            ToggleToolMode_Welder.Name = new StringBuilder("Enable Welder Mode");
            //ToggleToolMode_Welder.Writer = ToggleToolMode_IconWriter;
            ToggleToolMode_Welder.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            ToggleToolMode_Welder.InvalidToolbarTypes = new List<VRage.Game.MyToolbarType> { VRage.Game.MyToolbarType.Seat };
            ToggleToolMode_Welder.ValidForGroups = true;
            ToggleToolMode_Welder.Action = Block => BlockAction(Block, x => x.TermControls.ToolMode = EntityModules.LaserToolModules.LaserTerminalControlsModule.ToolModeWelder);
            return ToggleToolMode_Welder;
        }

        public IMyTerminalAction ToggleToolMode_Grinder()
        {
            IMyTerminalAction ToggleToolMode_Grinder = MyAPIGateway.TerminalControls.CreateAction<IMyShipGrinder>("ToggleToolMode_Grinder");
            ToggleToolMode_Grinder.Enabled = Block => HasBlockLogic(Block);
            ToggleToolMode_Grinder.Name = new StringBuilder("Enable Grinder Mode");
            //ToggleToolMode_Grinder.Writer = ToggleToolMode_IconWriter;
            ToggleToolMode_Grinder.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            ToggleToolMode_Grinder.InvalidToolbarTypes = new List<VRage.Game.MyToolbarType> { VRage.Game.MyToolbarType.Seat };
            ToggleToolMode_Grinder.ValidForGroups = true;
            ToggleToolMode_Grinder.Action = Block => BlockAction(Block, x => x.TermControls.ToolMode = EntityModules.LaserToolModules.LaserTerminalControlsModule.ToolModeGrinder);
            return ToggleToolMode_Grinder;
        }
        #endregion

        #region Property Definitions
        public IMyTerminalControlProperty<IReadOnlyDictionary<MyItemType, float>> MissingComponents()
        {
            IMyTerminalControlProperty<IReadOnlyDictionary<MyItemType, float>> MissingComponents = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyDictionary<MyItemType, float>, IMyShipGrinder>("MissingComponents");
            MissingComponents.Enabled = Block => HasBlockLogic(Block);
            MissingComponents.Getter = MissingComponents_Getter;
            MissingComponents.Setter = MissingComponents_Setter;

            return MissingComponents;
        }

        IReadOnlyDictionary<MyItemType, float> MissingComponents_Getter(IMyTerminalBlock Block)
        {
            return BlockReturn(Block, kernel => kernel.Responder.LastReportedMissingComponents);
        }

        void MissingComponents_Setter(IMyTerminalBlock Block, IReadOnlyDictionary<MyItemType, float> trash)
        {
            throw new InvalidOperationException("This property is read-only.");
        }
        #endregion
    }
}
