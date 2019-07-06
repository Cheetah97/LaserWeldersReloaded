using System;
using System.Collections.Generic;
using System.Text;
using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Utils;

namespace EemRdx.LaserWelders.SessionModules
{
    public abstract class SCTermControlsGeneratorModule<TShipController> : TerminalControlsGeneratorModuleBase<ILaserWeldersSessionKernel, SCKernel, TShipController> where TShipController : IMyShipController
    {
        public SCTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        protected override List<MyTerminalControlBatch> GenerateBatchControls()
        {
            List<MyTerminalControlBatch> Batch = new List<MyTerminalControlBatch>
            {
                Separator(),
                Label(), ShowHud2(), ShowBlocksOnHud2(), VerboseHud2(),
                HudPosX2(), HudPosY2(),
                Separator(),
            };

            return Batch;
        }

        protected override List<IMyTerminalControl> GenerateControls()
        {
            //return new List<IMyTerminalControl>
            //{
            //    //DefaultSeparator("LaserToolsTopSeparator", false),
            //    //DefaultLabel("LaserToolsLabel", "Laser Tools HUD", false),
            //    ////ShowHud(),
            //    ////ShowBlocksOnHud(),
            //    ////VerboseHud(),
            //    ////HudPosX(), HudPosY(),
            //    //DefaultSeparator("LaserToolsBottomSeparator", false),
            //};
            return new List<IMyTerminalControl>();
        }

        protected override List<IMyTerminalAction> GenerateActions()
        {
            //List<IMyTerminalAction> Actions = new List<IMyTerminalAction>();
            //Actions.AddRange(ToggleShowHud());
            //Actions.AddRange(ToggleShowBlocksOnHud());
            //Actions.AddRange(ToggleVerboseHud());
            //return Actions;
            return new List<IMyTerminalAction>();
        }

        private MyTerminalControlBatch Separator()
        {
            MyTerminalControlBatch Batch;
            Batch.Control = DefaultSeparator("LaserToolsTopSeparator", false);
            Batch.Actions = null;
            return Batch;
        }

        private MyTerminalControlBatch Label()
        {
            MyTerminalControlBatch Batch;
            Batch.Control = DefaultLabel("LaserToolsLabel", "Laser Tools HUD", false);
            Batch.Actions = null;
            return Batch;
        }

        #region Control Definitions
        private MyTerminalControlBatch ShowHud2()
        {
            DefaultOnOffControlTexts controlTexts;
            controlTexts.Title = "Show Laser Tool HUD";
            controlTexts.Tooltip = "If enabled, the ship controller will display installed laser tools' status on your HUD.";
            controlTexts.OnText = string.Empty;
            controlTexts.OffText = string.Empty;

            DefaultOnOffToggleActionTexts actionTexts = GenerateToggleTexts("Laser Tool HUD");

            Func<SCKernel, bool> getter = kernel => kernel.TermControls.ShowHud;
            Action<SCKernel, bool> setter = (kernel, NewMode) => kernel.TermControls.ShowHud = NewMode;
            return DefaultCheckbox("ShowLaserToolHud", controlTexts, actionTexts, true, getter, setter, InvalidForSeats);
        }

        //public IMyTerminalControlCheckbox ShowHud()
        //{
        //    IMyTerminalControlCheckbox ShowHud = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipController>("ShowLaserToolHud");
        //    ShowHud.SupportsMultipleBlocks = false;
        //    ShowHud.Enabled = HasBlockLogic;
        //    ShowHud.Visible = HasBlockLogic;
        //    ShowHud.Getter = (Block) => BlockReturn(Block, x => x.TermControls.ShowHud);
        //    ShowHud.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.ShowHud = NewMode);
        //    ShowHud.Title = MyStringId.GetOrCompute("Show Laser Tool HUD");
        //    ShowHud.Tooltip = MyStringId.GetOrCompute($"If enabled, the ship controller will display installed laser tools' status on your HUD.");
        //    return ShowHud;
        //}

        private MyTerminalControlBatch ShowBlocksOnHud2()
        {
            DefaultOnOffControlTexts controlTexts;
            controlTexts.Title = "Show Incomplete Blocks on HUD";
            controlTexts.Tooltip = "If enabled, the ship controller will display the names and positions of damaged and unbuilt blocks on the grids affected by Laser Welders.";
            controlTexts.OnText = string.Empty;
            controlTexts.OffText = string.Empty;

            DefaultOnOffToggleActionTexts actionTexts = GenerateToggleTexts("showing blocks on HUD");

            Func<SCKernel, bool> getter = kernel => kernel.TermControls.ShowBlocksOnHud;
            Action<SCKernel, bool> setter = (kernel, NewMode) => kernel.TermControls.ShowBlocksOnHud = NewMode;
            return DefaultCheckbox("ShowDamagedBlocksOnHud", controlTexts, actionTexts, true, getter, setter, InvalidForSeats);
        }

        //public IMyTerminalControlCheckbox ShowBlocksOnHud()
        //{
        //    IMyTerminalControlCheckbox ShowBlocksOnHud = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipController>("ShowDamagedBlocksOnHud");
        //    ShowBlocksOnHud.SupportsMultipleBlocks = false;
        //    ShowBlocksOnHud.Enabled = (Block) => HasBlockLogic(Block) && BlockReturn(Block, x => x.TermControls.ShowHud);
        //    ShowBlocksOnHud.Visible = HasBlockLogic;
        //    ShowBlocksOnHud.Getter = (Block) => BlockReturn(Block, x => x.TermControls.ShowBlocksOnHud);
        //    ShowBlocksOnHud.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.ShowBlocksOnHud = NewMode);
        //    ShowBlocksOnHud.Title = MyStringId.GetOrCompute("Show Incomplete Blocks on HUD");
        //    ShowBlocksOnHud.Tooltip = MyStringId.GetOrCompute($"If enabled, the ship controller will display the names and positions of damaged and unbuilt blocks on the grids affected by Laser Welders.");
        //    return ShowBlocksOnHud;
        //}

        private MyTerminalControlBatch VerboseHud2()
        {
            DefaultOnOffControlTexts controlTexts;
            controlTexts.Title = "Verbose HUD Info";
            controlTexts.Tooltip = "If enabled, the HUD will display very detailed information about each tool's work.";
            controlTexts.OnText = string.Empty;
            controlTexts.OffText = string.Empty;

            DefaultOnOffToggleActionTexts actionTexts = GenerateToggleTexts("verbose HUD");

            Func<SCKernel, bool> getter = kernel => kernel.TermControls.VerboseHud;
            Action<SCKernel, bool> setter = (kernel, NewMode) => kernel.TermControls.VerboseHud = NewMode;
            return DefaultCheckbox("LaserToolVerboseHud", controlTexts, actionTexts, true, getter, setter, InvalidForSeats);
        }

        //public IMyTerminalControlCheckbox VerboseHud()
        //{
        //    IMyTerminalControlCheckbox VerboseHud = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipController>("ShowLaserToolHud");
        //    VerboseHud.SupportsMultipleBlocks = false;
        //    VerboseHud.Enabled = HasBlockLogic;
        //    VerboseHud.Visible = HasBlockLogic;
        //    VerboseHud.Getter = (Block) => BlockReturn(Block, x => x.TermControls.VerboseHud);
        //    VerboseHud.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.VerboseHud = NewMode);
        //    VerboseHud.Title = MyStringId.GetOrCompute("Verbose HUD Info");
        //    VerboseHud.Tooltip = MyStringId.GetOrCompute($"If enabled, the HUD will display very detailed information about each tool's work.");
        //    return VerboseHud;
        //}

        private MyTerminalControlBatch HudPosX2()
        {
            DefaultSliderControlTexts controlTexts;
            controlTexts.Title = "HUD Position X";
            controlTexts.Tooltip = "Sets the X coordinate of the HUD position on the screen.";

            DefaultSliderActionTexts actionTexts;
            string textbase = controlTexts.Title;
            actionTexts.IncreaseName = $"Increase {textbase}";
            actionTexts.DecreaseName = $"Decrease {textbase}";

            Func<SCKernel, float> getter = kernel => kernel.TermControls.HudPosX;
            Action<SCKernel, float> setter = (kernel, NewX) => kernel.TermControls.HudPosX = NewX;
            Func<SCKernel, float> minGetter = kernel => -1;
            Func<SCKernel, float> maxGetter = kernel => 1;
            Func<float, string> writer = X => $"{Math.Round(X, 2).ToString()}";

            return DefaultSliderControl("LaserToolHudPosX", controlTexts, actionTexts, SliderLimits.Linear, true, getter, setter, minGetter, maxGetter, 0.05f, writer, InvalidForSeats);
        }

        private MyTerminalControlBatch HudPosY2()
        {
            DefaultSliderControlTexts controlTexts;
            controlTexts.Title = "HUD Position Y";
            controlTexts.Tooltip = "Sets the Y coordinate of the HUD position on the screen.";

            DefaultSliderActionTexts actionTexts;
            string textbase = controlTexts.Title;
            actionTexts.IncreaseName = $"Increase {textbase}";
            actionTexts.DecreaseName = $"Decrease {textbase}";

            Func<SCKernel, float> getter = kernel => kernel.TermControls.HudPosY;
            Action<SCKernel, float> setter = (kernel, NewY) => kernel.TermControls.HudPosY = NewY;
            Func<SCKernel, float> minGetter = kernel => -1;
            Func<SCKernel, float> maxGetter = kernel => 1;
            Func<float, string> writer = Y => $"{Math.Round(Y, 2).ToString()}";

            return DefaultSliderControl("LaserToolHudPosY", controlTexts, actionTexts, SliderLimits.Linear, true, getter, setter, minGetter, maxGetter, 0.05f, writer, InvalidForSeats);
        }

        //public IMyTerminalControlSlider HudPosX()
        //{
        //    IMyTerminalControlSlider HudPosX = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipController>("LaserToolHudPosX");
        //    HudPosX.Title = MyStringId.GetOrCompute("HUD Position X");
        //    HudPosX.Tooltip = MyStringId.GetOrCompute("Sets the X coordinate of the HUD position on the screen.");
        //    HudPosX.SupportsMultipleBlocks = false;
        //    HudPosX.Enabled = HasBlockLogic;
        //    HudPosX.Visible = HasBlockLogic;
        //    HudPosX.SetLimits(-1, 1);
        //    HudPosX.Getter = (Block) => BlockReturn(Block, x => x.TermControls.HudPosX);
        //    HudPosX.Setter = (Block, NewX) => BlockAction(Block, x => x.TermControls.HudPosX = NewX);
        //    HudPosX.Writer = (Block, Info) => Info.Append($"{BlockReturn(Block, x => Math.Round(x.TermControls.HudPosX, 2))}");
        //    return HudPosX;
        //}

        //public IMyTerminalControlSlider HudPosY()
        //{
        //    IMyTerminalControlSlider HudPosY = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipController>("LaserToolHudPosY");
        //    HudPosY.Title = MyStringId.GetOrCompute("HUD Position Y");
        //    HudPosY.Tooltip = MyStringId.GetOrCompute("Sets the Y coordinate of the HUD position on the screen.");
        //    HudPosY.SupportsMultipleBlocks = false;
        //    HudPosY.Enabled = HasBlockLogic;
        //    HudPosY.Visible = HasBlockLogic;
        //    HudPosY.SetLimits(-1, 1);
        //    HudPosY.Getter = (Block) => BlockReturn(Block, x => x.TermControls.HudPosY);
        //    HudPosY.Setter = (Block, NewY) => BlockAction(Block, x => x.TermControls.HudPosY = NewY);
        //    HudPosY.Writer = (Block, Info) => Info.Append($"{BlockReturn(Block, x => Math.Round(x.TermControls.HudPosY, 2))}");
        //    return HudPosY;
        //}
        #endregion

        private DefaultOnOffToggleActionTexts GenerateToggleTexts(string textbase)
        {
            DefaultOnOffToggleActionTexts texts;
            texts.ToggleTitle = $"Toggle {textbase}";
            texts.SwitchOnTitle = $"Enable {textbase}";
            texts.SwitchOffTitle = $"Disable {textbase}";
            texts.ToggledOnText = "On";
            texts.ToggledOffText = "Off";
            return texts;
        }

        //public List<IMyTerminalAction> ToggleShowHud()
        //{
        //    DefaultOnOffToggleActionTexts texts = GenerateToggleTexts("Laser Tool HUD");
        //    return DefaultOnOffToggleAction("LaserToolsShowHud", texts, false, kernel => kernel.TermControls.ShowHud, (kernel, val) => kernel.TermControls.ShowHud = val, new List<VRage.Game.MyToolbarType> { VRage.Game.MyToolbarType.Seat });
        //}

        //public List<IMyTerminalAction> ToggleShowBlocksOnHud()
        //{
        //    DefaultOnOffToggleActionTexts texts = GenerateToggleTexts("showing blocks on HUD");
        //    return DefaultOnOffToggleAction("LaserToolsShowBlocksOnHud", texts, false, kernel => kernel.TermControls.ShowBlocksOnHud, (kernel, val) => kernel.TermControls.ShowBlocksOnHud = val, new List<VRage.Game.MyToolbarType> { VRage.Game.MyToolbarType.Seat });
        //}

        //public List<IMyTerminalAction> ToggleVerboseHud()
        //{
        //    DefaultOnOffToggleActionTexts texts = GenerateToggleTexts("verbose HUD");
        //    return DefaultOnOffToggleAction("LaserToolsVerboseHud", texts, false, kernel => kernel.TermControls.VerboseHud, (kernel, val) => kernel.TermControls.VerboseHud = val, new List<VRage.Game.MyToolbarType> { VRage.Game.MyToolbarType.Seat });
        //}
    }

    public class CockpitTermControlsGeneratorModule : SCTermControlsGeneratorModule<IMyCockpit>
    {
        public CockpitTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(CockpitTermControlsGeneratorModule);
    }

    public class RCTermControlsGeneratorModule : SCTermControlsGeneratorModule<IMyRemoteControl>
    {
        public RCTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(RCTermControlsGeneratorModule);
    }
}
