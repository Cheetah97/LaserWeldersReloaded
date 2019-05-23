using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Serialization;
using VRageMath;

namespace EemRdx.LaserWelders
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(500, typeof(ToolTierCharacteristics))]
    [XmlInclude(typeof(ToolTierCharacteristics))]
    public class LaserSettings
    {
        [ProtoMember]
        public bool Debug = false;
        [ProtoMember]
        public bool AllowAsyncWelding = false;
        [ProtoMember]
        public float PowerMultiplier = 1;
        [ProtoMember]
        public float PowerScaleMultiplier = 1.2f;
        [ProtoMember]
        [XmlIgnore]
        public Dictionary<string, ToolTierCharacteristics> ToolTiers
        {
            get
            {
                return SerializableToolTiers.Dictionary;
            }
            set
            {
                SerializableToolTiers.Dictionary = value;
            }
        }
        /// <summary>Used only for XML serialization. Use <see cref="ToolTiers"/> instead.</summary>
        [ProtoIgnore]
        [XmlElement(ElementName = "ToolTiers")]
        public SerializableDictionary<string, ToolTierCharacteristics> SerializableToolTiers = new SerializableDictionary<string, ToolTierCharacteristics>();
        [ProtoMember]
        public float SpeedMultiplierPowerScaleMultiplier = 0.9f;
        [ProtoMember]
        public int BlockLimitsAssessmentPerTick = 2000;
        [ProtoMember]
        public bool EnableDrilling = true;

        public static LaserSettings Default
        {
            get
            {
                return new LaserSettings()
                {
                    ToolTiers = new Dictionary<string, ToolTierCharacteristics>
                    {
                        ["LargeShipLaserMultitool"] = new ToolTierCharacteristics
                        {
                            ToolSubtypeId = "LargeShipLaserMultitool",
                            MaxBeamLengthBlocks = 4,
                            DrillingVoxelsPerTick = 4,
                            WeldGrindSpeedMultiplier = 0.5f,
                            WelderGrinderWorkingZoneWidth = 2,
                            DrillingYield = 1,
                            DrillingZoneRadius = 2,
                            SpeedMultiplierMaxValue = 2,
                            InternalBeamColor = Color.WhiteSmoke,
                            ExternalWeldBeamColor = new Color(12, 127, 242, 255),
                            ExternalGrindBeamColor = new Color(230, 20, 20, 255),
                        },
                        ["SmallShipLaserMultitool"] = new ToolTierCharacteristics
                        {
                            ToolSubtypeId = "SmallShipLaserMultitool",
                            MaxBeamLengthBlocks = 20,
                            DrillingVoxelsPerTick = 4,
                            WeldGrindSpeedMultiplier = 0.5f,
                            WelderGrinderWorkingZoneWidth = 2,
                            DrillingYield = 1,
                            DrillingZoneRadius = 2,
                            SpeedMultiplierMaxValue = 2,
                            InternalBeamColor = Color.WhiteSmoke,
                            ExternalWeldBeamColor = new Color(12, 127, 242, 255),
                            ExternalGrindBeamColor = new Color(230, 20, 20, 255),
                        },
                        ["LargeShipLaserMultitoolMK2"] = new ToolTierCharacteristics
                        {
                            ToolSubtypeId = "LargeShipLaserMultitoolMK2",
                            MaxBeamLengthBlocks = 8,
                            DrillingVoxelsPerTick = 4,
                            WeldGrindSpeedMultiplier = 1,
                            WelderGrinderWorkingZoneWidth = 3,
                            DrillingYield = 1,
                            DrillingZoneRadius = 3,
                            SpeedMultiplierMaxValue = 4,
                            InternalBeamColor = Color.WhiteSmoke,
                            ExternalWeldBeamColor = Color.CadetBlue,
                            ExternalGrindBeamColor = new Color(230, 20, 20, 255),
                        },
                        ["SmallShipLaserMultitoolMK2"] = new ToolTierCharacteristics
                        {
                            ToolSubtypeId = "SmallShipLaserMultitoolMK2",
                            MaxBeamLengthBlocks = 40,
                            DrillingVoxelsPerTick = 4,
                            WeldGrindSpeedMultiplier = 1,
                            WelderGrinderWorkingZoneWidth = 3,
                            DrillingYield = 1,
                            DrillingZoneRadius = 3,
                            SpeedMultiplierMaxValue = 4,
                            InternalBeamColor = Color.WhiteSmoke,
                            ExternalWeldBeamColor = Color.CadetBlue,
                            ExternalGrindBeamColor = new Color(230, 20, 20, 255),
                        },
                    }
                };
            }
        }

        public bool IsValid()
        {
            return ToolTiers != null && ToolTiers.Count > 0 && ToolTiers.Values.All(x => x.MaxBeamLengthBlocks > 0 && x.SpeedMultiplierMaxValue > 0 && x.WelderGrinderWorkingZoneWidth > 0);
        }
    }


    [Serializable]
    [ProtoContract]
    public struct ToolTierCharacteristics
    {
        [ProtoMember(10)]
        public string ToolSubtypeId;
        [ProtoMember(20)]
        public int MaxBeamLengthBlocks;
        [ProtoMember(30)]
        public float WeldGrindSpeedMultiplier;
        [ProtoMember(40)]
        public int WelderGrinderWorkingZoneWidth;
        [ProtoMember(50)]
        public float DrillingZoneRadius;
        [ProtoMember(60)]
        public int DrillingVoxelsPerTick;
        [ProtoMember(70)]
        public int SpeedMultiplierMaxValue;
        [ProtoMember(80)]
        public float DrillingYield;
        [ProtoMember(90)]
        public Color InternalBeamColor;
        [ProtoMember(100)]
        public Color ExternalWeldBeamColor;
        [ProtoMember(110)]
        public Color ExternalGrindBeamColor;
    }
}
