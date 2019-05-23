using System;
using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface ISCTermControlsGenerator : ISessionModule
    {
        void EnsureCockpitControls();
        void EnsureRCControls();
    }

    public class SCTermControlsGeneratorModule : SessionModuleBase<ILaserWeldersSessionKernel>, ISCTermControlsGenerator
    {
        public SCTermControlsGeneratorModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(SCTermControlsGeneratorModule);

        private bool CockpitControlsInited = false;
        private bool RCControlsInited = false;

        public void EnsureCockpitControls()
        {
            if (CockpitControlsInited) return;
            CockpitControlsInited = true;

            GenerateControls<IMyCockpit>();
        }

        public void EnsureRCControls()
        {
            if (RCControlsInited) return;
            RCControlsInited = true;

            GenerateControls<IMyRemoteControl>();
        }

        private void GenerateControls<TShipController>() where TShipController: IMyShipController
        {
            MyAPIGateway.TerminalControls.AddControl<TShipController>(TopSeparator());
            MyAPIGateway.TerminalControls.AddControl<TShipController>(Label());
            MyAPIGateway.TerminalControls.AddControl<TShipController>(ShowHud());
            MyAPIGateway.TerminalControls.AddControl<TShipController>(VerboseHud());
            MyAPIGateway.TerminalControls.AddControl<TShipController>(HudPosX());
            MyAPIGateway.TerminalControls.AddControl<TShipController>(HudPosY());
            MyAPIGateway.TerminalControls.AddControl<TShipController>(BottomSeparator());

            MyAPIGateway.TerminalControls.AddAction<TShipController>(ToggleShowHud());
        }

        #region Term-to-Block Interfacing
        public bool HasBlockLogic(IMyTerminalBlock Block)
        {
            try
            {
                return Block.HasComponent<SCKernel>();
            }
            catch (Exception Scrap)
            {
                MySessionKernel.Log?.LogError($"{DebugModuleName}.HasBlockLogic", "HasComponent threw", Scrap, LoggingLevelEnum.Default);
                return false;
            }
        }

        public void BlockAction(IMyTerminalBlock Block, Action<SCKernel> Action)
        {
            try
            {
                SCKernel Logic;
                if (!Block.TryGetComponent(out Logic)) return;
                Action(Logic);
            }
            catch (Exception Scrap)
            {
                MySessionKernel.Log?.LogError($"{DebugModuleName}.BlockAction", "BlockAction threw", Scrap, LoggingLevelEnum.Default);
                return;
            }
        }

        public T BlockReturn<T>(IMyTerminalBlock Block, Func<SCKernel, T> Getter, T Default = default(T))
        {
            try
            {
                SCKernel Logic;
                if (!Block.TryGetComponent(out Logic)) return Default;
                return Getter(Logic);
            }
            catch (Exception Scrap)
            {
                MySessionKernel.Log?.LogError($"{DebugModuleName}.BlockReturn", "BlockReturn threw", Scrap, LoggingLevelEnum.Default);
                return Default;
            }
        }
        #endregion

        #region Control Definitions
        public IMyTerminalControlSeparator TopSeparator()
        {
            IMyTerminalControlSeparator TopSeparator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyShipController>("LaserToolsTopSeparator");
            TopSeparator.Enabled = HasBlockLogic;
            TopSeparator.Visible = HasBlockLogic;
            TopSeparator.SupportsMultipleBlocks = true;
            return TopSeparator;
        }

        public IMyTerminalControlLabel Label()
        {
            IMyTerminalControlLabel Label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyShipController>("LaserToolsLabel");
            Label.Enabled = HasBlockLogic;
            Label.Visible = HasBlockLogic;
            Label.SupportsMultipleBlocks = true;
            Label.Label = MyStringId.GetOrCompute($"Laser Tools HUD");

            return Label;
        }

        public IMyTerminalControlCheckbox ShowHud()
        {
            IMyTerminalControlCheckbox ShowHud = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipController>("ShowLaserToolHud");
            ShowHud.SupportsMultipleBlocks = true;
            ShowHud.Enabled = HasBlockLogic;
            ShowHud.Visible = HasBlockLogic;
            ShowHud.Getter = (Block) => BlockReturn(Block, x => x.TermControls.ShowHud);
            ShowHud.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.ShowHud = NewMode);
            ShowHud.Title = MyStringId.GetOrCompute("Show Laser Tool HUD");
            ShowHud.Tooltip = MyStringId.GetOrCompute($"If enabled, the ship controller will display installed laser tools' status on your HUD.");
            return ShowHud;
        }

        public IMyTerminalControlCheckbox VerboseHud()
        {
            IMyTerminalControlCheckbox VerboseHud = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyShipController>("ShowLaserToolHud");
            VerboseHud.SupportsMultipleBlocks = true;
            VerboseHud.Enabled = HasBlockLogic;
            VerboseHud.Visible = HasBlockLogic;
            VerboseHud.Getter = (Block) => BlockReturn(Block, x => x.TermControls.VerboseHud);
            VerboseHud.Setter = (Block, NewMode) => BlockAction(Block, x => x.TermControls.VerboseHud = NewMode);
            VerboseHud.Title = MyStringId.GetOrCompute("Verbose HUD Info");
            VerboseHud.Tooltip = MyStringId.GetOrCompute($"If enabled, the HUD will display very detailed information about each tool's work.");
            return VerboseHud;
        }

        public IMyTerminalControlSlider HudPosX()
        {
            IMyTerminalControlSlider HudPosX = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipController>("LaserToolHudPosX");
            HudPosX.Title = MyStringId.GetOrCompute("HUD Position X");
            HudPosX.Tooltip = MyStringId.GetOrCompute("Sets the X coordinate of the HUD position on the screen.");
            HudPosX.SupportsMultipleBlocks = true;
            HudPosX.Enabled = HasBlockLogic;
            HudPosX.Visible = HasBlockLogic;
            HudPosX.SetLimits(-1, 1);
            HudPosX.Getter = (Block) => BlockReturn(Block, x => x.TermControls.HudPosX);
            HudPosX.Setter = (Block, NewX) => BlockAction(Block, x => x.TermControls.HudPosX = NewX);
            HudPosX.Writer = (Block, Info) => Info.Append($"{BlockReturn(Block, x => Math.Round(x.TermControls.HudPosX, 2))}");
            return HudPosX;
        }

        public IMyTerminalControlSlider HudPosY()
        {
            IMyTerminalControlSlider HudPosY = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipController>("LaserToolHudPosY");
            HudPosY.Title = MyStringId.GetOrCompute("HUD Position Y");
            HudPosY.Tooltip = MyStringId.GetOrCompute("Sets the Y coordinate of the HUD position on the screen.");
            HudPosY.SupportsMultipleBlocks = true;
            HudPosY.Enabled = HasBlockLogic;
            HudPosY.Visible = HasBlockLogic;
            HudPosY.SetLimits(-1, 1);
            HudPosY.Getter = (Block) => BlockReturn(Block, x => x.TermControls.HudPosY);
            HudPosY.Setter = (Block, NewY) => BlockAction(Block, x => x.TermControls.HudPosY = NewY);
            HudPosY.Writer = (Block, Info) => Info.Append($"{BlockReturn(Block, x => Math.Round(x.TermControls.HudPosY, 2))}");
            return HudPosY;
        }

        public IMyTerminalControlSeparator BottomSeparator()
        {
            IMyTerminalControlSeparator BottomSeparator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyShipController>("LaserToolsBottomSeparator");
            BottomSeparator.Enabled = HasBlockLogic;
            BottomSeparator.Visible = HasBlockLogic;
            BottomSeparator.SupportsMultipleBlocks = true;
            return BottomSeparator;
        }
        #endregion

        public IMyTerminalAction ToggleShowHud()
        {
            IMyTerminalAction ToggleShowHud = MyAPIGateway.TerminalControls.CreateAction<IMyShipController>("LaserToolsToggleShowHud");
            ToggleShowHud.Enabled = HasBlockLogic;
            ToggleShowHud.Name = new System.Text.StringBuilder("Toggle Laser Tool HUD");
            //ToggleToolMode.Writer = ToggleToolMode_IconWriter;
            ToggleShowHud.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            ToggleShowHud.InvalidToolbarTypes = new System.Collections.Generic.List<VRage.Game.MyToolbarType> { VRage.Game.MyToolbarType.Seat };
            ToggleShowHud.ValidForGroups = true;
            ToggleShowHud.Action = Block => BlockAction(Block, x => x.TermControls.ShowHud = !x.TermControls.ShowHud);
            return ToggleShowHud;
        }
    }
}
