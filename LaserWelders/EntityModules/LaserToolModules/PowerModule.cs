using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Utils;
using EemRdx.EntityModules;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IPowerModule : IEntityModule
    {
        bool SufficientPower { get; }
        float PowerRatio { get; }
        float RequiredInput { get; }
    }

    public class PowerModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, ClosableModule, UpdatableModule, IPowerModule
    {
        private float PowerConsumptionMultiplier => (MyKernel?.Session?.Settings != null ? MyKernel.Session.Settings.PowerMultiplier : 1);
        static MyDefinitionId Electricity { get; } = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity");
        static MyDefinitionId Hydrogen { get; } = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen");
        MyDefinitionId UsedPowerType { get; } = Electricity;
        float UsedPowerTypePowerDensity = 1;

        public PowerModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(PowerModule);

        public bool Inited { get; } = false;

        public bool RequiresOperable { get; } = false;

        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_10TH_FRAME;
        private MyResourceSinkComponent Sink;

        #region IPowerModule
        public bool SufficientPower
        {
            get
            {
                if (Sink != null) return Sink.SuppliedRatioByType(UsedPowerType) >= 0.95f;
                else return false;
            }
        }
        public float PowerRatio
        {
            get
            {
                if (Sink != null) return Sink.SuppliedRatioByType(UsedPowerType);
                else return 0;
            }
        }
        public float RequiredInput
        {
            get
            {
                if (Sink != null) return Sink.RequiredInputByType(UsedPowerType);
                else return 0;
            }
        }
        #endregion

        private float maxinput
        {
            get
            {
                LaserSettings settings = MyKernel.Session?.Settings != null ? MyKernel.Session.Settings : LaserSettings.Default;
                string subtypeId = MyKernel.Block.BlockDefinition.SubtypeName;
                int maxbeamlength = MyKernel.Session?.Settings?.ToolTiers?.ContainsKey(subtypeId) == true ? MyKernel.Session.Settings.ToolTiers[subtypeId].MaxBeamLengthBlocks : 1;
                float gridSize = MyKernel.Block.CubeGrid.GridSize;
                return (maxbeamlength * gridSize) * PowerConsumptionMultiplier / UsedPowerTypePowerDensity;
            }
        }

        void InitializableModule.Init()
        {
            float? powerDensity = MyKernel.Session.GasPowerDensityProvider.GetGasPowerDensitySafe(UsedPowerType);
            if (!powerDensity.HasValue)
                throw new InvalidOperationException($"Cannot find energy density for power resource '{(UsedPowerType != null ? UsedPowerType.SubtypeName : "null")}'");
            UsedPowerTypePowerDensity = powerDensity.HasValue ? powerDensity.Value : 1;
            Sink = MyKernel.Tool.Components.Get<MyResourceSinkComponent>();
            if (UsedPowerType != Electricity)
            {
                MyDefinitionId electricity = Electricity;
                Sink.RemoveType(ref electricity);
                VRage.Game.Components.MyResourceSinkInfo info = new VRage.Game.Components.MyResourceSinkInfo
                {
                    ResourceTypeId = UsedPowerType,
                    MaxRequiredInput = maxinput,
                    RequiredInputFunc = GetPowerConsumptionMW
                };
                Sink.AddType(ref info);
            }
            else
            {
                Sink.SetMaxRequiredInputByType(UsedPowerType, maxinput);
                Sink.SetRequiredInputFuncByType(UsedPowerType, GetPowerConsumptionMW);
            }
            Sink.Update();
            MyKernel.Block.AppendingCustomInfo += Block_AppendingCustomInfo;
        }

        private void Block_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder info)
        {
            info.AppendLine($"Max required input: {Math.Round(maxinput, 2)} MW");
            info.AppendLine($"Current required input: {Math.Round(GetPowerConsumptionMW(), 2)} MW");
        }

        private float GetPowerConsumptionMW()
        {
            if (!MyKernel.Toggle.Toggled) return 0.0001f / UsedPowerTypePowerDensity; // 100 W

            int SpeedMultiplier = MyKernel.TermControls.SpeedMultiplier;
            float BeamLengthM = MyKernel.TermControls.LaserBeamLength * MyKernel.Tool.CubeGrid.GridSize;
            float Base = MyKernel.Session.Settings.PowerScaleMultiplier;
            float SpeedBase = MyKernel.Session.Settings.SpeedMultiplierPowerScaleMultiplier;
            float SpeedMult = MyKernel.TermControls.SpeedMultiplier != 1 ? (float)(SpeedMultiplier * (Math.Pow(SpeedBase, SpeedMultiplier) / SpeedBase)) : 1;
            return (float)(Math.Pow(Base, BeamLengthM) * SpeedMult * PowerConsumptionMultiplier / UsedPowerTypePowerDensity);
        }

        void ClosableModule.Close()
        {
            MyKernel.Block.AppendingCustomInfo -= Block_AppendingCustomInfo;
        }

        void UpdatableModule.Update()
        {
            //Sink.SetRequiredInputByType(UsedPowerType, GetPowerConsumptionMW());
            Sink.Update();
            MyKernel.Tool.RefreshCustomInfo();
        }
    }
}
