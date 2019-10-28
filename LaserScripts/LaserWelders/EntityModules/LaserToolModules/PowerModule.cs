using Sandbox.ModAPI;
using System;
using VRage.Utils;
using EemRdx.EntityModules;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public class PowerModule : PowerModuleBase<ILaserWeldersSessionKernel, ILaserToolKernel, IMyShipWelder>
    {
        public PowerModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        private float PowerConsumptionMultiplier => (MyKernel?.Session?.Settings != null ? MyKernel.Session.Settings.PowerMultiplier : 1);

        public override float MaxRequiredInput => CalculatePowerConsumption(MyKernel.BeamDrawer.MaxBeamLengthM, MyKernel.TermControls.SpeedMultiplier);

        protected override bool ModifyBlockInfo { get; } = true;

        protected override bool UpdateMaxInput { get; } = true;
        
        protected override MyStringHash SinkPriority { get; } = MyStringHash.GetOrCompute("Utility");

        /// <summary>
        /// 100 W
        /// </summary>
        public readonly float MinimumPowerConsumption = 1 / 1000 / 1000 * 100;

        protected override float GetPowerConsumptionMW()
        {
            if (!MyKernel.Toggle.Toggled) return MinimumPowerConsumption / UsedPowerTypePowerDensity;

            int SpeedMultiplier = MyKernel.TermControls.SpeedMultiplier;
            float BeamLengthM = MyKernel.TermControls.LaserBeamLength * MyKernel.Tool.CubeGrid.GridSize;
            return CalculatePowerConsumption(BeamLengthM, SpeedMultiplier);
        }

        private float CalculatePowerConsumption(float BeamLengthM, int SpeedMultiplier)
        {
            float Base = MyKernel.Session.Settings.PowerScaleMultiplier;
            float SpeedBase = MyKernel.Session.Settings.SpeedMultiplierPowerScaleMultiplier;
            float SpeedMult = (float)(SpeedMultiplier * (Math.Pow(SpeedBase, SpeedMultiplier) / SpeedBase));
            return (float)(Math.Pow(Base, BeamLengthM) * SpeedMult * PowerConsumptionMultiplier / UsedPowerTypePowerDensity);
        }
    }
        //public interface IPowerModule : IEntityModule
        //{
        //    bool SufficientPower { get; }
        //    float PowerRatio { get; }
        //    float RequiredInput { get; }
        //    float MaxRequiredInput { get; }
        //}

        //public class PowerModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, ClosableModule, UpdatableModule, IPowerModule
        //{
        //    private float PowerConsumptionMultiplier => (MyKernel?.Session?.Settings != null ? MyKernel.Session.Settings.PowerMultiplier : 1);
        //    private MyDefinitionId Electricity { get; } = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity");
        //    private MyDefinitionId Hydrogen { get; } = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen");
        //    MyDefinitionId UsedPowerType => Electricity;
        //    float UsedPowerTypePowerDensity = 1;

        //    public PowerModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        //    public override string DebugModuleName { get; } = nameof(PowerModule);

        //    public bool Inited { get; } = false;

        //    public bool RequiresOperable { get; } = false;

        //    public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_10TH_FRAME;
        //    private MyResourceSinkComponent Sink;

        //    #region IPowerModule
        //    public float SufficiencyRatio { get; } = 0.95f;
        //    public bool SufficientPower
        //    {
        //        get
        //        {
        //            if (Sink != null) return Sink.SuppliedRatioByType(UsedPowerType) >= SufficiencyRatio;
        //            else return false;
        //        }
        //    }
        //    public float PowerRatio
        //    {
        //        get
        //        {
        //            if (Sink != null) return Sink.SuppliedRatioByType(UsedPowerType);
        //            else return 0;
        //        }
        //    }
        //    public float RequiredInput
        //    {
        //        get
        //        {
        //            if (Sink != null) return Sink.RequiredInputByType(UsedPowerType);
        //            else return 0;
        //        }
        //    }
        //    public float MaxRequiredInput
        //    {
        //        get
        //        {
        //            LaserSettings settings = MyKernel.Session?.Settings != null ? MyKernel.Session.Settings : LaserSettings.Default;
        //            string subtypeId = MyKernel.Block.BlockDefinition.SubtypeName;
        //            int maxbeamlength = MyKernel.Session?.Settings?.ToolTiers?.ContainsKey(subtypeId) == true ? MyKernel.Session.Settings.ToolTiers[subtypeId].MaxBeamLengthBlocks : 1;
        //            float gridSize = MyKernel.Block.CubeGrid.GridSize;
        //            return (maxbeamlength * gridSize) * PowerConsumptionMultiplier / UsedPowerTypePowerDensity;
        //        }
        //    }
        //    #endregion

        //    void InitializableModule.Init()
        //    {
        //        float? powerDensity = MyKernel.Session.GasPowerDensityProvider.GetGasPowerDensitySafe(UsedPowerType);
        //        if (!powerDensity.HasValue)
        //            throw new InvalidOperationException($"Cannot find energy density for power resource '{(UsedPowerType != null ? UsedPowerType.SubtypeName : "null")}'");
        //        UsedPowerTypePowerDensity = powerDensity.HasValue ? powerDensity.Value : 1;
        //        Sink = MyKernel.Tool.Components.Get<MyResourceSinkComponent>();
        //        if (UsedPowerType != Electricity)
        //        {
        //            MyDefinitionId electricity = Electricity;
        //            Sink.RemoveType(ref electricity);
        //            VRage.Game.Components.MyResourceSinkInfo info = new VRage.Game.Components.MyResourceSinkInfo
        //            {
        //                ResourceTypeId = UsedPowerType,
        //                MaxRequiredInput = MaxRequiredInput,
        //                RequiredInputFunc = GetPowerConsumptionMW
        //            };
        //            Sink.AddType(ref info);
        //        }
        //        else
        //        {
        //            Sink.SetMaxRequiredInputByType(UsedPowerType, MaxRequiredInput);
        //            Sink.SetRequiredInputFuncByType(UsedPowerType, GetPowerConsumptionMW);
        //        }
        //        Sink.Update();
        //        MyKernel.Block.AppendingCustomInfo += Block_AppendingCustomInfo;
        //    }

        //    private void Block_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder info)
        //    {
        //        info.AppendLine($"Max required input: {Math.Round(MaxRequiredInput, 2)} MW");
        //        info.AppendLine($"Current required input: {Math.Round(GetPowerConsumptionMW(), 2)} MW");
        //    }

        //    private float GetPowerConsumptionMW()
        //    {
        //        if (!MyKernel.Toggle.Toggled) return 0.0001f / UsedPowerTypePowerDensity; // 100 W

        //        int SpeedMultiplier = MyKernel.TermControls.SpeedMultiplier;
        //        float BeamLengthM = MyKernel.TermControls.LaserBeamLength * MyKernel.Tool.CubeGrid.GridSize;
        //        float Base = MyKernel.Session.Settings.PowerScaleMultiplier;
        //        float SpeedBase = MyKernel.Session.Settings.SpeedMultiplierPowerScaleMultiplier;
        //        float SpeedMult = MyKernel.TermControls.SpeedMultiplier != 1 ? (float)(SpeedMultiplier * (Math.Pow(SpeedBase, SpeedMultiplier) / SpeedBase)) : 1;
        //        return (float)(Math.Pow(Base, BeamLengthM) * SpeedMult * PowerConsumptionMultiplier / UsedPowerTypePowerDensity);
        //    }

        //    void ClosableModule.Close()
        //    {
        //        MyKernel.Block.AppendingCustomInfo -= Block_AppendingCustomInfo;
        //    }

        //    void UpdatableModule.Update()
        //    {
        //        //Sink.SetRequiredInputByType(UsedPowerType, GetPowerConsumptionMW());
        //        Sink.Update();
        //        MyKernel.Tool.RefreshCustomInfo();
        //    }
        //}
}
