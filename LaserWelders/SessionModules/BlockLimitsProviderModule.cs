using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EemRdx.LaserWelders.Models;
using EemRdx.Networking;
using EemRdx.SessionModules;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using MyBlockLimitsEnabledEnum = VRage.Game.MyBlockLimitsEnabledEnum;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface IBlockLimitsProvider : ISessionModule
    {
        IReadOnlyDictionary<string, short> BlockLimits { get; }
        MyBlockLimitsEnabledEnum BlockLimitsUsed { get; }
        int MaxBlocksPerPlayer { get; }
        int MaxGridBlocks { get; }
        int TotalPCU { get; }

        IBlockLimitsData BlocksData { get; }

        bool CheckBlockLimits(MyCubeBlockDefinition BlockDefinition, long PlayerIdentityId, IMyCubeGrid GridToAdd = null);
        bool CheckBlockLimits(IMySlimBlock Block, long PlayerIdentityId, IMyCubeGrid GridToAdd = null);

        void AddBlock(IMySlimBlock Block, long? PlayerId = null);
        void RemoveBlock(IMySlimBlock Block, long? PlayerId = null);
    }

    public class BlockLimitsProviderModule : SessionModuleBase<ILaserWeldersSessionKernel>, InitializableModule, UpdatableModule, IBlockLimitsProvider
    {
        public BlockLimitsProviderModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(BlockLimitsProviderModule);
        private int Ticker => MySessionKernel.Clock.Ticker;
        private bool BlockLimitsEnabled = false;

        public IReadOnlyDictionary<string, short> BlockLimits { get; private set; } = new Dictionary<string, short>();
        public MyBlockLimitsEnabledEnum BlockLimitsUsed { get; private set; } = MyBlockLimitsEnabledEnum.NONE;

        public IReadOnlyDictionary<MyDefinitionId, int> BlockPCUDefinitions { get; private set; } = new Dictionary<MyDefinitionId, int>();
        public IReadOnlyDictionary<MyDefinitionId, string> BlockPairnameDefinitions { get; private set; } = new Dictionary<MyDefinitionId, string>();

        public int MaxBlocksPerPlayer { get; private set; } = int.MaxValue;
        public int MaxGridBlocks { get; private set; } = int.MaxValue;
        public int TotalPCU { get; private set; } = int.MaxValue;

        IBlockLimitsData IBlockLimitsProvider.BlocksData => BlocksData;
        public BlockLimitsData BlocksData
        {
            get
            {
                if (BlockDataSyncer == null) return new BlockLimitsData();
                return BlockDataSyncer.Data;
            }
        }
        private Sync<BlockLimitsData> BlockDataSyncer;

        void InitializableModule.Init()
        {
            BlockDataSyncer = new Sync<BlockLimitsData>(MySessionKernel.Networker, "BlockLimitsData", new BlockLimitsData());
            MyObjectBuilder_SessionSettings SessionSettings = MyAPIGateway.Session.GetWorld().Checkpoint.Settings;
            BlockLimitsUsed = SessionSettings.BlockLimitsEnabled;
            BlockLimitsEnabled = BlockLimitsUsed.HasFlag(MyBlockLimitsEnabledEnum.PER_PLAYER) || BlockLimitsUsed.HasFlag(MyBlockLimitsEnabledEnum.PER_FACTION) || BlockLimitsUsed.HasFlag(MyBlockLimitsEnabledEnum.GLOBALLY);
            BlockLimits = SessionSettings.BlockTypeLimits.Dictionary;
            MaxBlocksPerPlayer = SessionSettings.MaxBlocksPerPlayer;
            MaxGridBlocks = SessionSettings.MaxGridSize;
            TotalPCU = SessionSettings.TotalPCU;

            List<MyCubeBlockDefinition> AllDefinitions = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().ToList();
            var BlockPCUDefinitions = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
            var BlockPairnameDefinitions = new Dictionary<MyDefinitionId, string>(MyDefinitionId.Comparer);
            foreach (var BlockDef in AllDefinitions)
            {
                BlockPCUDefinitions.Add(BlockDef.Id, BlockDef.PCU);
                BlockPairnameDefinitions.Add(BlockDef.Id, BlockDef.BlockPairName);
            }
            this.BlockPCUDefinitions = BlockPCUDefinitions;
            this.BlockPairnameDefinitions = BlockPairnameDefinitions;

            WriteToLog("Init", $"{nameof(BlockLimitsProviderModule)} initialized, {AllDefinitions.Count} definitions processed", LoggingLevelEnum.DebugLog, true, 10000);
        }

        IEnumerator<bool> LimitsUpdater = null;
        void UpdatableModule.Update()
        {
            if (Ticker == 10 || Ticker % (15 * 60) == 0)
            {
                if (BlockLimitsEnabled)
                {
                    LimitsUpdater = UpdateLimits();
                }
            }

            if (!MyAPIGateway.Session.IsServer && Ticker % (5 * 60) == 0)
            {
                BlockDataSyncer.Ask();
            }

            if (MyAPIGateway.Session.IsServer && LimitsUpdater != null)
            {
                LimitsUpdater.MoveNext();
                bool working = LimitsUpdater.Current;
                if (!working)
                {
                    LimitsUpdater.Dispose();
                    LimitsUpdater = null;
                    try
                    {
                        PrintLimits();
                    }
                    catch (Exception Scrap)
                    {
                        LogError("Update", "PrintLimits threw", Scrap);
                    }
                }
            }
        }

        void PrintLimits()
        {
            List<IMyIdentity> identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities);
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine($"");
            builder.AppendLine($"--- Real data ---");
            builder.AppendLine($"Current block limit use:");
            builder.AppendLine($"");
            WriteToLog("PrintLimits", builder.ToString());
            builder.Clear();
            foreach (IMyPlayer player in players)
            {
                builder.AppendLine($"Player {player.DisplayName}:");
                builder.AppendLine($"SteamID: {player.SteamUserId}");
                builder.AppendLine($"IdentityID: {player.IdentityId}");
                //builder.AppendLine($"");

                PlayerBlockLimits playerLimits = BlocksData.PlayerLimits.FirstOrDefault(x => x.IdentityId == player.IdentityId);
                if (playerLimits == null)
                {
                    builder.AppendLine($"No data");
                    continue;
                }
                builder.AppendLine($"PCU: {playerLimits.UsedPCU}");
                builder.AppendLine($"Block types:");
                if (playerLimits.UsedBlockPairs != null)
                {
                    if (playerLimits.UsedBlockPairs.Count > 0)
                    {
                        foreach (var kvp2 in playerLimits.UsedBlockPairs)
                        {
                            builder.AppendLine($"{kvp2.Key}: {kvp2.Value}");
                        }
                    }
                    else
                    {
                        builder.AppendLine($"Empty");
                    }
                }
                else
                {
                    builder.AppendLine($"N/A");
                }
                builder.AppendLine($"");
            }

            builder.AppendLine($"");
            builder.AppendLine($"");
            WriteToLog("PrintLimits", builder.ToString());
            builder.Clear();
            BlockLimitsData serialized = MyAPIGateway.Utilities.SerializeFromBinary<BlockLimitsData>(MyAPIGateway.Utilities.SerializeToBinary(BlocksData));

            builder.AppendLine($"--- Serialized data ---");
            builder.AppendLine($"Current block limit use:");
            builder.AppendLine($"");
            foreach (IMyPlayer player in players)
            {
                builder.AppendLine($"Player {player.DisplayName}:");
                builder.AppendLine($"SteamID: {player.SteamUserId}");
                builder.AppendLine($"IdentityID: {player.IdentityId}");
                builder.AppendLine($"");

                PlayerBlockLimits playerLimits = serialized.PlayerLimits.FirstOrDefault(x => x.IdentityId == player.IdentityId);
                if (playerLimits == null)
                {
                    builder.AppendLine($"No data");
                    continue;
                }
                builder.AppendLine($"PCU: {playerLimits.UsedPCU}");
                builder.AppendLine($"Block types:");
                if (playerLimits.UsedBlockPairs != null)
                {
                    if (playerLimits.UsedBlockPairs.Count > 0)
                    {
                        foreach (var kvp2 in playerLimits.UsedBlockPairs)
                        {
                            builder.AppendLine($"{kvp2.Key}: {kvp2.Value}");
                        }
                    }
                    else
                    {
                        builder.AppendLine($"Empty");
                    }
                }
                else
                {
                    builder.AppendLine($"N/A");
                }
                builder.AppendLine($"");
            }

            builder.AppendLine($"");
            builder.AppendLine($"");

            WriteToLog("PrintLimits", builder.ToString());
        }

        IEnumerator<bool> UpdateLimits()
        {
            Stopwatch TotalWatch = Stopwatch.StartNew();

            bool threadCompleted = false;
            BlockLimitsData newBlockData = null;
            Action threadRunner = () => UpdateLimits_Thread(out newBlockData);
            Action threadCallback = () => threadCompleted = true;
            MyAPIGateway.Parallel.StartBackground(threadRunner, threadCallback);

            while (!threadCompleted) yield return true;
            BlocksData.PlayerLimits.Clear();
            BlocksData.PlayerLimits.AddRange(newBlockData.PlayerLimits);
            BlocksData.FactionLimits.Clear();
            BlocksData.FactionLimits.AddRange(newBlockData.FactionLimits);
            BlocksData.GlobalLimits = newBlockData.GlobalLimits;
            BlockDataSyncer.Update();
            
            //WriteToLog("UpdateLimits", $"Update took: {Math.Round(TotalWatch.Elapsed.TotalMilliseconds, 3)}ms");

            yield return false;
        }

        private void UpdateLimits_Thread(out BlockLimitsData newBlockData)
        {
            MyObjectBuilder_World World = MyAPIGateway.Session.GetWorld();
            Dictionary<long, MyObjectBuilder_Identity> IdentitiesOB = World.Checkpoint.Identities.ToDictionary(x => x.IdentityId);
            List<MyObjectBuilder_CubeGrid> GridsOB = World.Sector.SectorObjects.OfType<MyObjectBuilder_CubeGrid>().ToList();
            Dictionary<long, long> PlayersAndTheirFactions = new Dictionary<long, long>();
            Dictionary<string, short> zeroedBlockLimits = new Dictionary<string, short>();
            foreach (var kvp in BlockLimits)
            {
                zeroedBlockLimits.Add(kvp.Key, 0);
            }

            newBlockData = new BlockLimitsData();
            Dictionary<long, PlayerBlockLimits> PlayerBlocksData = new Dictionary<long, PlayerBlockLimits>();
            Dictionary<long, FactionBlockLimits> FactionBlockData = new Dictionary<long, FactionBlockLimits>();
            GlobalBlockLimits globalBlockLimits = null;

            foreach (MyObjectBuilder_Identity Player in IdentitiesOB.Values)
            {
                var pbl = new PlayerBlockLimits(Player.IdentityId);
                pbl.UsedBlockPairs = new Dictionary<string, short>(zeroedBlockLimits);
                PlayerBlocksData.Add(Player.IdentityId, pbl);

                IMyFaction PlayerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Player.IdentityId);
                if (PlayerFaction != null)
                {
                    PlayersAndTheirFactions.Add(Player.IdentityId, PlayerFaction.FactionId);
                    if (!FactionBlockData.ContainsKey(PlayerFaction.FactionId))
                    {
                        var fbl = new FactionBlockLimits(PlayerFaction.FactionId);
                        fbl.UsedBlockPairs = new Dictionary<string, short>(zeroedBlockLimits);
                        FactionBlockData.Add(PlayerFaction.FactionId, fbl);
                    }
                }
            }
            globalBlockLimits = new GlobalBlockLimits();

            int totalBlocksFullyProcessed = 0;
            foreach (MyObjectBuilder_CubeGrid Grid in GridsOB)
            {
                var SlimBlocks = Grid.CubeBlocks;

                foreach (var Block in SlimBlocks)
                {
                    MyDefinitionId blockDefId = Block.GetId();
                    long playerId = Block.BuiltBy;
                    long playerFactionId = 0;
                    PlayersAndTheirFactions.TryGetValue(playerId, out playerFactionId);
                    PlayerBlockLimits playerBlockLimits;
                    if (!PlayerBlocksData.TryGetValue(playerId, out playerBlockLimits)) continue;
                    FactionBlockLimits factionBlockLimits;
                    FactionBlockData.TryGetValue(playerFactionId, out factionBlockLimits);

                    ModifyDictionaries(1, blockDefId, playerBlockLimits, factionBlockLimits, globalBlockLimits);
                    totalBlocksFullyProcessed++;
                    //if (totalBlocks > 0 && totalBlocks % MySessionKernel.Settings.BlockLimitsAssessmentPerTick == 0)
                    //    yield return true;
                }
            }
            newBlockData.PlayerLimits = PlayerBlocksData.Values.ToList();
            newBlockData.FactionLimits = FactionBlockData.Values.ToList();
            newBlockData.GlobalLimits = globalBlockLimits;
        }

        public void AddBlock(IMySlimBlock Block, long? PlayerId = null)
        {
            if (Block == null) throw new ArgumentNullException(nameof(Block));
            ChangeBlockLimits(1, Block, PlayerId);
        }

        public void RemoveBlock(IMySlimBlock Block, long? PlayerId = null)
        {
            if (Block == null) throw new ArgumentNullException(nameof(Block));
            ChangeBlockLimits(-1, Block, PlayerId);
        }

        private void ChangeBlockLimits(short opSign, IMySlimBlock Block, long? PlayerId = null)
        {
            if (Block == null) throw new ArgumentNullException(nameof(Block));
            MyDefinitionId blockDefId = Block.BlockDefinition.Id;

            if (PlayerId == null)
            {
                if (Block.FatBlock != null)
                {
                    PlayerId = (Block.FatBlock as Sandbox.Game.Entities.MyCubeBlock).BuiltBy;
                }
                else
                {
                    var BlockOB = Block.GetObjectBuilder();
                    PlayerId = BlockOB.BuiltBy;
                }
            }

            if (PlayerId == 0) return;
            long PlayerFactionId = 0;
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(PlayerId.Value);
            if (faction != null) PlayerFactionId = faction.FactionId;

            PlayerBlockLimits playerBlockLimits = BlocksData.PlayerLimits.FirstOrDefault(x => x.IdentityId == PlayerId);
            if (playerBlockLimits == null) return;
            FactionBlockLimits factionBlockLimits = PlayerFactionId != 0 ? BlocksData.FactionLimits.FirstOrDefault(x => x.FactionId == PlayerFactionId) : null;
            GlobalBlockLimits globalBlockLimits = BlocksData.GlobalLimits;

            ModifyDictionaries(opSign, blockDefId, playerBlockLimits, factionBlockLimits, globalBlockLimits);
        }

        private void ModifyDictionaries(short opSign, MyDefinitionId blockDefId, PlayerBlockLimits playerBlockLimits, FactionBlockLimits factionBlockLimits, GlobalBlockLimits globalBlockLimits)
        {
            int blockPCU = 0;
            string pairname = null;
            if (!BlockPCUDefinitions.TryGetValue(blockDefId, out blockPCU)) return;
            if (!BlockPairnameDefinitions.TryGetValue(blockDefId, out pairname)) return;

            if (playerBlockLimits.UsedBlockPairs.ContainsKey(pairname))
                playerBlockLimits.UsedBlockPairs[pairname] += (short)(1 * opSign);

            playerBlockLimits.UsedBlockCount += (short)(1 * opSign);
            playerBlockLimits.UsedPCU += blockPCU * opSign;


            if (factionBlockLimits != null)
            {
                if (factionBlockLimits.UsedBlockPairs.ContainsKey(pairname))
                    factionBlockLimits.UsedBlockPairs[pairname] += (short)(1 * opSign);

                factionBlockLimits.UsedBlockCount += (short)(1 * opSign);
                factionBlockLimits.UsedPCU += blockPCU * opSign;
            }

            if (globalBlockLimits.UsedBlockPairs.ContainsKey(pairname))
                globalBlockLimits.UsedBlockPairs[pairname] += (short)(1 * opSign);

            globalBlockLimits.UsedBlockCount += (short)(1 * opSign);
            globalBlockLimits.UsedPCU += blockPCU * opSign;
        }

        /// <summary>
        /// Checks whether it is possible to add a block to a player.
        /// </summary>
        /// <param name="Block">The block which is going to be placed (e.g. from a projected grid).</param>
        /// <param name="PlayerIdentityId"><see cref="IMyIdentity.IdentityId"/> of the player who is going to own the block.</param>
        /// <param name="GridToAdd">The cube grid on which the block is supposed to be added. If specified, the function will also check whether the block will fit under grid max block count limits.</param>
        /// <returns>Whether the block will fit under current limits or not. Note that if <paramref name="PlayerIdentityId"/> does not resolve to a valid Player Identity Id, only global limit checks will be performed, and player- and faction limit checks will be assumed as true.</returns>
        public bool CheckBlockLimits(IMySlimBlock Block, long PlayerIdentityId, IMyCubeGrid GridToAdd = null)
        {
            if (Block == null) throw new ArgumentNullException(nameof(Block));
            return CheckBlockLimits((Block.BlockDefinition as MyCubeBlockDefinition), PlayerIdentityId, GridToAdd);
        }

        /// <summary>
        /// Checks whether it is possible to add a block to a player.
        /// </summary>
        /// <param name="BlockDefinition">The definition of the block which is going to be placed.</param>
        /// <param name="PlayerIdentityId"><see cref="IMyIdentity.IdentityId"/> of the player who is going to own the block.</param>
        /// <param name="GridToAdd">The cube grid on which the block is supposed to be added. If specified, the function will also check whether the block will fit under grid max block count limits.</param>
        /// <returns>Whether the block will fit under current limits or not. Note that if <paramref name="PlayerIdentityId"/> does not resolve to a valid Player Identity Id, only global limit checks will be performed, and player- and faction limit checks will be assumed as true.</returns>
        public bool CheckBlockLimits(MyCubeBlockDefinition BlockDefinition, long PlayerIdentityId, IMyCubeGrid GridToAdd = null)
        {
            if (BlockDefinition == null) throw new ArgumentNullException(nameof(BlockDefinition));
            if (BlockLimitsUsed == MyBlockLimitsEnabledEnum.NONE) return true;

            IPlayerBlockLimits PlayerLimits = BlocksData.PlayerLimits.FirstOrDefault(x => x.IdentityId == PlayerIdentityId);
            IFactionBlockLimits FactionLimits = null;
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(PlayerIdentityId);
            long factionId = 0;
            if (faction != null)
            {
                factionId = faction.FactionId;
                FactionLimits = BlocksData.FactionLimits.FirstOrDefault(x => x.FactionId == factionId);
            }
            IGlobalBlockLimits GlobalBlockLimits = BlocksData.GlobalLimits;

            if (PlayerLimits != null && PlayerLimits.UsedBlockCount >= MaxBlocksPerPlayer) return false;
            if (GridToAdd != null)
            {
                if ((GridToAdd as Sandbox.Game.Entities.MyCubeGrid).BlocksCount >= MaxGridBlocks) return false;
            }

            if (BlockLimitsUsed == MyBlockLimitsEnabledEnum.PER_PLAYER && PlayerLimits != null)
            {
                IReadOnlyDictionary<string, short> _playerUsedBlocks = PlayerLimits.UsedBlockPairs;
                string pairname = BlockDefinition.BlockPairName;

                if (_playerUsedBlocks != null && BlockLimits.ContainsKey(pairname))
                {
                    short blocklimit = BlockLimits[pairname];
                    short usedblocklimit = _playerUsedBlocks[pairname];
                    if (usedblocklimit >= blocklimit)
                    {
                        //WriteToLog("CheckBlockLimits", $"Block {Block.BlockDefinition.Id.SubtypeName} fails to pass block limits ({usedblocklimit}/{blocklimit})", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, showOnHud: true);
                        return false;
                    }
                }

                if (PlayerLimits.UsedPCU + BlockDefinition.PCU > TotalPCU)
                {
                    //WriteToLog("CheckBlockLimits", $"Block {Block.BlockDefinition.Id.SubtypeName} fails to pass PCU limits ({PlayerUsedPCU + BlockDefinition.PCU}/{BlockLimits.TotalPCU})", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, showOnHud: true);
                    return false;
                }
            }

            if (BlockLimitsUsed == MyBlockLimitsEnabledEnum.PER_FACTION && FactionLimits != null)
            {
                IReadOnlyDictionary<string, short> _factionUsedBlocks = FactionLimits.UsedBlockPairs;
                string pairname = BlockDefinition.BlockPairName;

                if (_factionUsedBlocks != null && BlockLimits.ContainsKey(pairname))
                {
                    short blocklimit = BlockLimits[pairname];
                    short usedblocklimit = _factionUsedBlocks[pairname];
                    if (usedblocklimit >= blocklimit)
                    {
                        //WriteToLog("CheckBlockLimits", $"Block {Block.BlockDefinition.Id.SubtypeName} fails to pass block limits ({usedblocklimit}/{blocklimit})", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, showOnHud: true);
                        return false;
                    }
                }

                if (FactionLimits.UsedPCU + BlockDefinition.PCU > TotalPCU)
                {
                    //WriteToLog("CheckBlockLimits", $"Block {Block.BlockDefinition.Id.SubtypeName} fails to pass PCU limits ({PlayerUsedPCU + BlockDefinition.PCU}/{BlockLimits.TotalPCU})", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, showOnHud: true);
                    return false;
                }
            }

            if (BlockLimitsUsed == MyBlockLimitsEnabledEnum.GLOBALLY && GlobalBlockLimits != null)
            {
                string pairname = BlockDefinition.BlockPairName;

                if (BlockLimits.ContainsKey(pairname))
                {
                    short blocklimit = BlockLimits[pairname];
                    short usedblocklimit = GlobalBlockLimits.UsedBlockPairs[pairname];
                    if (usedblocklimit >= blocklimit)
                    {
                        //WriteToLog("CheckBlockLimits", $"Block {Block.BlockDefinition.Id.SubtypeName} fails to pass block limits ({usedblocklimit}/{blocklimit})", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, showOnHud: true);
                        return false;
                    }
                }

                if (GlobalBlockLimits.UsedPCU + BlockDefinition.PCU > TotalPCU)
                {
                    //WriteToLog("CheckBlockLimits", $"Block {Block.BlockDefinition.Id.SubtypeName} fails to pass PCU limits ({PlayerUsedPCU + BlockDefinition.PCU}/{BlockLimits.TotalPCU})", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, showOnHud: true);
                    return false;
                }
            }

            return true;
        }
    }
}
