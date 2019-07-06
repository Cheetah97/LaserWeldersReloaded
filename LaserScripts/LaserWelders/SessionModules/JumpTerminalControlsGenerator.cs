using System;
using System.Collections.Generic;
using EemRdx.Extensions;
using EemRdx.SessionModules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace EemRdx.LaserWelders.SessionModules
{
    public class JumpTerminalControlsGenerator : TerminalControlsGeneratorModuleBase<ILaserWeldersSessionKernel, JumpKernel, IMyJumpDrive>
    {
        public JumpTerminalControlsGenerator(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(JumpTerminalControlsGenerator);

        protected override List<MyTerminalControlBatch> GenerateBatchControls()
        {
            return new List<MyTerminalControlBatch>();
        }

        protected override List<IMyTerminalAction> GenerateActions()
        {
            if (MySessionKernel.Settings.BonusFeature_JumpDriveInstantRechargeButtonForCreative)
            {
                return new List<IMyTerminalAction>
                {
                    CreativeChargeAction()
                };
            }
            else
            {
                return new List<IMyTerminalAction>();
            }
        }

        protected override List<IMyTerminalControl> GenerateControls()
        {
            if (MySessionKernel.Settings.BonusFeature_JumpDriveInstantRechargeButtonForCreative)
            {
                return new List<IMyTerminalControl>
                {
                    CreativeChargeButton()
                };
            }
            else
            {
                return new List<IMyTerminalControl>();
            }
        }

        private IMyTerminalControlButton CreativeChargeButton()
        {
            IMyTerminalControlButton Button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyJumpDrive>("LaserJumpRecharge");
            Button.Enabled = Block => HasBlockLogic(Block) && OwnerHasCreativeRights(Block);
            Button.Visible = Button.Enabled;
            Button.Title = MyStringId.GetOrCompute("Creative Recharge");
            Button.Tooltip = MyStringId.GetOrCompute("Instantly recharges the jump drive.\r\nOnly available if the owner has Creative Rights.");
            Button.Action = Block =>
            {
                IMyJumpDrive jumpDrive = Block as IMyJumpDrive;
                jumpDrive.CurrentStoredPower = jumpDrive.MaxStoredPower;
            };
            return Button;
        }

        private IMyTerminalAction CreativeChargeAction()
        {
            IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyJumpDrive>("LaserJumpRechargeAction");
            action.Enabled = Block => HasBlockLogic(Block) && OwnerHasCreativeRights(Block);
            action.Name = new System.Text.StringBuilder("Creative Recharge");
            action.Icon = EemRdx.Helpers.ActionIcons.SwitchOn;
            action.Action = Block =>
            {
                IMyJumpDrive jumpDrive = Block as IMyJumpDrive;
                jumpDrive.CurrentStoredPower = jumpDrive.MaxStoredPower;
            };
            return action;
        }

        private bool OwnerHasCreativeRights(IMyTerminalBlock Block)
        {
            if (MyAPIGateway.Session.CreativeMode) return true;
            IMyPlayer Owner = MyAPIGateway.Players.GetPlayerById(Block.OwnerId);
            if (Owner == null) return false;
            bool HasSpaceMasterRights = (int)Owner.PromoteLevel >= 3; // Space Master and above
            return HasSpaceMasterRights;
        }
    }
}
