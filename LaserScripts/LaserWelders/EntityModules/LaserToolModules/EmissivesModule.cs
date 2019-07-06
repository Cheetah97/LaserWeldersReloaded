using EemRdx.EntityModules;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public class EmissivesModule : EntityModuleBase<ILaserToolKernel>, UpdatableModule
    {
        public EmissivesModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(EmissivesModule);
        IMyFunctionalBlock Tool => (MyKernel.Entity as IMyFunctionalBlock);
        public bool RequiresOperable => false;

        public MyEntityUpdateEnum UpdateFrequency => MyEntityUpdateEnum.EACH_10TH_FRAME;

        void UpdatableModule.Update()
        {
            if (MyKernel.Tool.IsFunctional)
            {
                if (MyKernel.Toggle.Toggled)
                {
                    if (MyKernel.PowerModule.SufficientPower)
                    {
                        if (MyKernel.TermControls.ToolMode == LaserTerminalControlsModule.ToolModeWelder)
                        {
                            Tool.SetEmissiveParts("Emissive", MyKernel.BeamDrawer.ExternalWeldBeamColor, 1);
                        }
                        else if (MyKernel.TermControls.ToolMode == LaserTerminalControlsModule.ToolModeGrinder)
                        {
                            Tool.SetEmissiveParts("Emissive", MyKernel.BeamDrawer.ExternalGrindBeamColor, 1);
                        }
                    }
                    else
                    {
                        Tool.SetEmissiveParts("Emissive", EmissiveColors.LaserRed, 0.7f);
                    }
                }
                else
                {
                    Tool.SetEmissiveParts("Emissive", EmissiveColors.Disabled, 0.7f);
                }
            }
            else
            {
                Tool.SetEmissiveParts("Emissive", EmissiveColors.Damaged, 1);
            }
        }
    }
}

namespace EemRdx.LaserWelders
{
    public static class EmissiveColors
    {
        public static Color LaserBlue { get; } = new Color(12, 127, 242, 255);
        public static Color LaserRed { get; } = new Color(230, 20, 20, 255);
        public static Color LaserGold { get; } = Color.Gold;
        public static Color Damaged { get; } = Color.DarkRed;
        public static Color Disabled { get; } = Color.Black;
    }
}