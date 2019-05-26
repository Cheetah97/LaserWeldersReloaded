using EemRdx.EntityModules;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IPersistence : IEntityModule
    {
        LaserToolPersistentStruct Saved { get; }
    }

    public class PersistenceModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, ClosableModule, IPersistence
    {
        public PersistenceModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(PersistenceModule);

        public bool Inited { get; private set; }

        public LaserToolPersistentStruct Saved { get; private set; } = Fallback;
        public static LaserToolPersistentStruct Fallback { get; } = new LaserToolPersistentStruct
        {
            BeamLength = 4,
            DistanceBased = true,
            SpeedMultiplier = 1,
            ToolMode = true,
        };

        void InitializableModule.Init()
        {
            Inited = true;
            EnsureSavingComponent();
            Load();
            MyKernel.SessionBase.SaveProvider.Subscribe(Save);
        }

        void ClosableModule.Close()
        {
            MyKernel.SessionBase.SaveProvider?.Unsubscribe(Save);
        }

        private void Save()
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
                return;
            LaserToolPersistentStruct persistent = MyKernel.TermControls.CurrentSettings;
            string Raw = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(persistent));
            MyKernel.Block.Storage.SetValue(MyKernel.SessionBase.StorageGuid, Raw);
            WriteToLog("Save", $"Storage: {Raw}");
        }

        private void Load()
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer) return;

            string storage = MyKernel.Block.Storage[MyKernel.SessionBase.StorageGuid];
            if (string.IsNullOrWhiteSpace(storage))
            {
                WriteToLog("Load", $"Storage.Guid is empty");
                return;
            }
            WriteToLog("Load", $"Storage: {storage}");
            Saved = MyAPIGateway.Utilities.SerializeFromBinary<LaserToolPersistentStruct>(Convert.FromBase64String(storage));
        }

        private void EnsureSavingComponent()
        {
            if (MyKernel.Block.Storage?.ContainsKey(MyKernel.SessionBase.StorageGuid) != true)
            {
                if (MyKernel.Block.Storage == null)
                {
                    MyKernel.Block.Storage = new MyModStorageComponent();
                    WriteToLog("EnsureSavingComponent", $"Storage is null, creating new Storage");
                }
                MyKernel.Block.Storage.Add(MyKernel.SessionBase.StorageGuid, string.Empty);
                WriteToLog("EnsureSavingComponent", $"Storage is empty, adding Guid to Storage");
            }
        }
    }

    #region Struct
    [ProtoContract]
    public struct LaserToolPersistentStruct
    {
        [ProtoMember(1)]
        public int BeamLength;
        [ProtoMember(2)]
        public bool DistanceBased;
        [ProtoMember(3)]
        public int SpeedMultiplier;
        [ProtoMember(4)]
        public bool ToolMode;
        [ProtoMember(5)]
        public bool DumpStone;
    }
    #endregion
}
