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
    [ProtoInclude(2700, typeof(SimpleColor))]
    [XmlInclude(typeof(ToolTierCharacteristics))]
    [XmlInclude(typeof(ToolTierCharacteristics))]
    public class LaserSettings : EemRdx.SessionModules.ISessionSettings
    {
        [ProtoMember(1)]
        public bool Debug = false;
        [ProtoIgnore]
        [XmlIgnore]
        public bool AllowAsyncWelding = false;
        [ProtoMember(2)]
        public float PowerMultiplier = 1;
        [ProtoMember(3)]
        public float PowerScaleMultiplier = 1.2f;
        [ProtoMember(4)]
        public float SpeedMultiplierPowerScaleMultiplier = 0.9f;
        [ProtoMember(5)]
        public bool EnableDrilling = true;
        [ProtoMember(10)]
        public bool SpeedMultiplierAcceleratesDrilling = true;
        [ProtoMember(6)]
        public int MaxToolUpdatePerTick = 4;
        [ProtoMember(9)]
        public int MaxInventoryUpdatePerTick = 10;
        [ProtoMember(7)]
        public bool PreventUseInCombat = true;
        [ProtoMember(8)]
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
        [ProtoMember(11)]
        public bool BonusFeature_JumpDriveInstantRechargeButtonForCreative = true;

        public static LaserSettings Default
        {
            get
            {
                bool upsideDown = false;
                if (upsideDown)
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
                                SpeedMultiplierMaxValue = 1,
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
                                SpeedMultiplierMaxValue = 1,
                                InternalBeamColor = Color.WhiteSmoke,
                                ExternalWeldBeamColor = new Color(12, 127, 242, 255),
                                ExternalGrindBeamColor = new Color(230, 20, 20, 255),
                            },
                            ["LargeShipLaserMultitoolMK2"] = new ToolTierCharacteristics
                            {
                                ToolSubtypeId = "LargeShipLaserMultitoolMK2",
                                MaxBeamLengthBlocks = 8,
                                DrillingVoxelsPerTick = 6,
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
                                DrillingVoxelsPerTick = 6,
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
                else
                {
                    return new LaserSettings()
                    {
                        ToolTiers = new Dictionary<string, ToolTierCharacteristics>
                        {
                            ["LargeShipLaserMultitool"] = new ToolTierCharacteristics
                            {
                                ToolSubtypeId = "LargeShipLaserMultitool",
                                MaxBeamLengthBlocks = 8,
                                DrillingVoxelsPerTick = 4,
                                WeldGrindSpeedMultiplier = 1,
                                WelderGrinderWorkingZoneWidth = 2,
                                DrillingYield = 1,
                                DrillingZoneRadius = 2,
                                SpeedMultiplierMaxValue = 4,
                                InternalBeamColor = Color.WhiteSmoke,
                                ExternalWeldBeamColor = new Color(12, 127, 242, 255),
                                ExternalGrindBeamColor = new Color(230, 20, 20, 255),
                            },
                            ["SmallShipLaserMultitool"] = new ToolTierCharacteristics
                            {
                                ToolSubtypeId = "SmallShipLaserMultitool",
                                MaxBeamLengthBlocks = 40,
                                DrillingVoxelsPerTick = 4,
                                WeldGrindSpeedMultiplier = 1,
                                WelderGrinderWorkingZoneWidth = 2,
                                DrillingYield = 1,
                                DrillingZoneRadius = 2,
                                SpeedMultiplierMaxValue = 4,
                                InternalBeamColor = Color.WhiteSmoke,
                                ExternalWeldBeamColor = new Color(12, 127, 242, 255),
                                ExternalGrindBeamColor = new Color(230, 20, 20, 255),
                            },
                        }
                    };
                }
            }
        }

        public bool IsValid()
        {
            return MaxToolUpdatePerTick > 0 && ToolTiers != null && ToolTiers.Count > 0 && ToolTiers.Values.All(x => x.MaxBeamLengthBlocks >= 4 && x.SpeedMultiplierMaxValue >= 1 && x.WelderGrinderWorkingZoneWidth >= 2 && x.WeldGrindSpeedMultiplier >= 0.33f);
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
        public SimpleColor InternalBeamColor;
        [ProtoMember(100)]
        public SimpleColor ExternalWeldBeamColor;
        [ProtoMember(110)]
        public SimpleColor ExternalGrindBeamColor;

        public static ToolTierCharacteristics GetDefault(string subtypeId)
        {
            return new ToolTierCharacteristics
            {
                ToolSubtypeId = subtypeId,
                MaxBeamLengthBlocks = 4,
                DrillingVoxelsPerTick = 4,
                WeldGrindSpeedMultiplier = 0.5f,
                WelderGrinderWorkingZoneWidth = 2,
                DrillingYield = 1,
                DrillingZoneRadius = 2,
                SpeedMultiplierMaxValue = 1,
                InternalBeamColor = Color.WhiteSmoke,
                ExternalWeldBeamColor = new Color(12, 127, 242, 255),
                ExternalGrindBeamColor = new Color(230, 20, 20, 255),
            };
        }
    }

    [Serializable]
    [ProtoContract]
    public struct SimpleColor
    {
        [ProtoMember(1)]
        public int R;
        [ProtoMember(2)]
        public int G;
        [ProtoMember(3)]
        public int B;

        public static implicit operator Color(SimpleColor simpleColor)
        {
            return new Color(simpleColor.R, simpleColor.G, simpleColor.B, 255);
        }

        public static implicit operator SimpleColor(Color color)
        {
            SimpleColor simpleColor;
            simpleColor.R = color.R;
            simpleColor.G = color.G;
            simpleColor.B = color.B;
            return simpleColor;
        }
    }
}
