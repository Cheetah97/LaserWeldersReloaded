using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EemRdx.SessionModules;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using MyBlockLimitsEnabledEnum = VRage.Game.MyBlockLimitsEnabledEnum;

namespace EemRdx.LaserWelders.Models
{
    public interface IBlockLimits
    {
        IReadOnlyDictionary<string, short> UsedBlockPairs { get; }
        int UsedPCU { get; }
        int UsedBlockCount { get; }
    }

    [ProtoContract]
    [ProtoInclude(100, typeof(GlobalBlockLimits))]
    [ProtoInclude(200, typeof(PlayerBlockLimits))]
    [ProtoInclude(300, typeof(FactionBlockLimits))]
    public abstract class BlockLimitsBase : IBlockLimits
    {
        [ProtoMember(10)]
        public Dictionary<string, short> UsedBlockPairs = new Dictionary<string, short>();
        [ProtoMember(20)]
        public int UsedPCU;
        [ProtoMember(50)]
        public int UsedBlockCount;

        [ProtoIgnore]
        IReadOnlyDictionary<string, short> IBlockLimits.UsedBlockPairs => UsedBlockPairs;
        [ProtoIgnore]
        int IBlockLimits.UsedPCU => UsedPCU;
        [ProtoIgnore]
        int IBlockLimits.UsedBlockCount => UsedBlockCount;

        public BlockLimitsBase() { }
    }

    public interface IGlobalBlockLimits : IBlockLimits { }
    [ProtoContract]
    public class GlobalBlockLimits : BlockLimitsBase, IGlobalBlockLimits
    {
        public GlobalBlockLimits() : base() { }
    }

    public interface IPlayerBlockLimits : IBlockLimits
    {
        long IdentityId { get; }
    }
    [ProtoContract]
    public class PlayerBlockLimits : BlockLimitsBase, IPlayerBlockLimits
    {
        [ProtoMember(30)]
        public long IdentityId { get; private set; }
        public PlayerBlockLimits(long IdentityId)
        {
            this.IdentityId = IdentityId;
        }

        public PlayerBlockLimits() : base() { }
    }

    public interface IFactionBlockLimits : IBlockLimits
    {
        long FactionId { get; }
    }
    [ProtoContract]
    public class FactionBlockLimits : BlockLimitsBase, IFactionBlockLimits
    {
        [ProtoMember(40)]
        public long FactionId { get; private set; }
        public FactionBlockLimits(long FactionId)
        {
            this.FactionId = FactionId;
        }

        public FactionBlockLimits() : base() { }
    }

    public interface IBlockLimitsData
    {
        IReadOnlyList<IPlayerBlockLimits> PlayerLimits { get; }
        IReadOnlyList<FactionBlockLimits> FactionLimits { get; }
        IGlobalBlockLimits GlobalLimits { get; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(PlayerBlockLimits))]
    [ProtoInclude(1500, typeof(FactionBlockLimits))]
    [ProtoInclude(2000, typeof(GlobalBlockLimits))]
    public class BlockLimitsData : IBlockLimitsData
    {
        [ProtoIgnore]
        IReadOnlyList<IPlayerBlockLimits> IBlockLimitsData.PlayerLimits => PlayerLimits;
        [ProtoIgnore]
        IReadOnlyList<FactionBlockLimits> IBlockLimitsData.FactionLimits => FactionLimits;
        [ProtoIgnore]
        IGlobalBlockLimits IBlockLimitsData.GlobalLimits => GlobalLimits;

        [ProtoMember(1)]
        public List<PlayerBlockLimits> PlayerLimits = new List<PlayerBlockLimits>();
        [ProtoMember(2)]
        public List<FactionBlockLimits> FactionLimits = new List<FactionBlockLimits>();
        [ProtoMember(3)]
        public GlobalBlockLimits GlobalLimits = new GlobalBlockLimits();

        public BlockLimitsData() { }
    }
}
