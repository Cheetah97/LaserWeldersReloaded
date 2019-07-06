using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Utils;
using EemRdx.EntityModules;
using VRage.ModAPI;
using Draygo.API;
using Sandbox.Game.EntityComponents;
using System.Linq;
using System.Collections.Generic;
using LaserToolStatus = EemRdx.LaserWelders.EntityModules.LaserToolModules.LaserToolStatus;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRageMath;
using EemRdx.Extensions;
using VRage.Game.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public interface IHUDModule : IEntityModule
    {
        bool SCControlledByLocalPlayer { get; }
        StringBuilder Display { get; }
    }

    public class HUDModule : EntityModuleBase<ISCKernel>, InitializableModule, UpdatableModule, ClosableModule, IHUDModule
    {
        public HUDModule(ISCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(HUDModule);
        public bool Inited { get; private set; } = false;
        public bool RequiresOperable { get; } = false;
        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;

        private int Ticker => MyKernel.Session.Clock.Ticker;
        private HudAPIv2 HudAPI => MyKernel.Session?.HUDAPIProvider?.HudAPI;
        private HudAPIv2.HUDMessage HUDPanel;
        //private HudAPIv2.BillBoardHUDMessage HUDPanelBackground;
        private bool inited = false;
        public StringBuilder Display { get; private set; } = new StringBuilder();
        private IReadOnlyList<ILaserToolKernel> Tools => MyKernel.ToolListProvider?.Tools;
        private readonly StringBuilder customInfo = new StringBuilder();
        private readonly MyStringId MATERIAL_VANILLA_SQUARE = MyStringId.GetOrCompute("Square");
        private Vector2D DefaultOrigin = new Vector2D(-0.95f, 0.95);
        public bool SCControlledByLocalPlayer => MyAPIGateway.Session?.LocalHumanPlayer?.Controller?.ControlledEntity == MyKernel.SC;

        void InitializableModule.Init()
        {
            var _default = MyKernel.Persistence.Default;
            DefaultOrigin = new Vector2D(_default.HudPosX, _default.HudPosY);
            MyKernel.Block.AppendingCustomInfo += Block_AppendingCustomInfo;
        }

        private void Block_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder _info)
        {
            _info.AppendLine();
            _info.Append(customInfo.ToString());
        }

        void ClosableModule.Close()
        {
            MyKernel.Block.AppendingCustomInfo -= Block_AppendingCustomInfo;
        }

        private void init()
        {
            if (inited || !HudAPI.Heartbeat) return;
            HUDPanel = new HudAPIv2.HUDMessage(Display, DefaultOrigin, Blend: BlendTypeEnum.SDR);
            //HUDPanelBackground = new HudAPIv2.BillBoardHUDMessage(MATERIAL_VANILLA_SQUARE, DefaultOrigin, VRageMath.Color.DeepSkyBlue);
            HUDPanel.Scale = 1.4;
            inited = true;
        }

        void UpdatableModule.Update()
        {
            customInfo.Clear();
            customInfo.Append("Text HUD unavailable");
            if (!inited)
            {
                if (HudAPI?.Heartbeat == true) init();
                return;
            }

            if (Ticker % 10 == 0)
            {
                if (CanUpdateHUD())
                    UpdateHUD();
                else
                    DisableHUD();

                MyKernel.Block.RefreshCustomInfo();
            }
        }

        bool CanUpdateHUD()
        {
            if (!inited || !HudAPI.Heartbeat) return false;
            if (Tools == null) return false;
            if (!SCControlledByLocalPlayer) return false;
            

            return MyKernel.TermControls?.ShowHud == true;
        }

        private void DisableHUD()
        {
            Display.Clear();
            HUDPanel.Visible = false;
        }

        private void UpdateHUD()
        {
            Display.Clear();
            
            if (!HUDPanel.Visible) HUDPanel.Visible = true;
            Vector2D desiredPosition = new Vector2D(MyKernel.TermControls.HudPosX, MyKernel.TermControls.HudPosY);
            if (desiredPosition != HUDPanel.Origin) HUDPanel.Origin = desiredPosition;
            //if (!HUDPanelBackground.Visible) HUDPanelBackground.Visible = true;
            customInfo.Clear();
            string ToolsOnboardLine = $"Laser tools onboard: {(Tools.Count > 0 ? Tools.Count.ToString() : "none")}";
            customInfo.AppendLine(ToolsOnboardLine);
            
            Display.AppendLine(ToolsOnboardLine);
            if (Tools.Count > 0)
            {
                Dictionary<LaserToolStatus, int> toolGroups = GroupToolsByStatus();
                string toolsByStatus = FormatToolStatusLine(toolGroups);
                Display.AppendLine(toolsByStatus);
                Display.Append(PrintMissingComponents().ToString());

                if (MyKernel.TermControls.VerboseHud)
                {
                    Display.AppendLine();
                    Display.AppendLine();

                    foreach (ILaserToolKernel Tool in Tools)
                    {
                        Display.AppendLine(PrintToolVerboseStatus(Tool).ToString());
                    }
                }
            }
        }

        private Dictionary<LaserToolStatus, int> GroupToolsByStatus()
        {
            Dictionary<LaserToolStatus, int> toolGroups = new Dictionary<LaserToolStatus, int>();
            toolGroups.Add(LaserToolStatus.Damaged, 0);
            toolGroups.Add(LaserToolStatus.Online, 0);
            toolGroups.Add(LaserToolStatus.OutOfPower, 0);
            toolGroups.Add(LaserToolStatus.Standby, 0);
            foreach (var tool in MyKernel.ToolListProvider.Tools)
            {
                LaserToolStatus status = tool.Responder.ToolStatus;
                toolGroups[status] += 1;
            }
            return toolGroups;
        }

        private static string ReadableLaserToolStatus(LaserToolStatus status, bool Capitalize)
        {
            if (status == LaserToolStatus.Online)
                return Capitalize ? "Online" : "online";
            if (status == LaserToolStatus.Standby)
                return Capitalize ? "Standby" : "standby";
            if (status == LaserToolStatus.OutOfPower)
                return Capitalize ? "Out of power" : "out of power";
            if (status == LaserToolStatus.Damaged)
                return Capitalize ? "Damaged" : "damaged";

            return "";
        }

        private static string FormatToolStatusLine(Dictionary<LaserToolStatus, int> toolGroups)
        {
            string onlineStatus = $"online: {toolGroups[LaserToolStatus.Online]}";
            string standbyStatus = toolGroups[LaserToolStatus.Standby] > 0 ? $"; standby: {toolGroups[LaserToolStatus.Standby]}" : "";
            string outofpowerStatus = toolGroups[LaserToolStatus.OutOfPower] > 0 ? $"; out of power: {toolGroups[LaserToolStatus.OutOfPower]}" : "";
            string damagedStatus = toolGroups[LaserToolStatus.Damaged] > 0 ? $"; damaged: {toolGroups[LaserToolStatus.Damaged]}" : "";

            return $"({onlineStatus}{standbyStatus}{outofpowerStatus}{damagedStatus})";
        }

        private StringBuilder PrintToolVerboseStatus(ILaserToolKernel Tool)
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine($"{Tool.Block.CustomName}: {ReadableLaserToolStatus(Tool.Responder.ToolStatus, false)}");
            info.AppendLine($"Mode: {(Tool.TermControls.ToolMode ? "Welder" : "Grinder")}");
            info.AppendLine($"Powered: {Math.Round(Tool.PowerModule.PowerRatio * 100)}%");
            //info.AppendLine($"Req. input: {Math.Round(Tool.PowerModule.RequiredInput, 3)}u");
            if (Tool.Responder.ToolStatusReport.Length > 0)
            {
                info.Append(Tool.Responder.ToolStatusReport.ToString());
                info.AppendLine();
            }
            info.AppendLine($"Support inventories: {Tool.Inventory.InventoryOwners.Count}");
            if (MyKernel.Session.Settings.Debug)
            {
                foreach (var Owner in Tool.Inventory.InventoryOwners)
                {
                    info.AppendLine(Owner.CustomName);
                }
            }
            info.AppendLine();

            return info;
        }

        private StringBuilder PrintMissingComponents()
        {
            Dictionary<MyItemType, float> TotalMissingComponents = new Dictionary<MyItemType, float>();
            StringBuilder Print = new StringBuilder();

            foreach (ILaserToolKernel Tool in Tools)
            {
                foreach (KeyValuePair<MyItemType, float> kvp in Tool.Responder.LastReportedMissingComponents)
                {
                    if (!TotalMissingComponents.ContainsKey(kvp.Key))
                        TotalMissingComponents.Add(kvp.Key, kvp.Value);
                    else
                        TotalMissingComponents[kvp.Key] += kvp.Value;
                }
            }

            if (TotalMissingComponents.Count == 0) return Print;
            Print.AppendLine($"Missing components:");
            foreach (KeyValuePair<MyItemType, float> MissingComponent in TotalMissingComponents)
            {
                Print.AppendLine($"{MissingComponent.Key.SubtypeId}: {MissingComponent.Value}");
            }

            return Print;
        }
    }
}
