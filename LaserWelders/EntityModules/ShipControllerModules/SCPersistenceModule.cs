using EemRdx.EntityModules;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public interface ISCPersistence : IEntityModule
    {
        SCPersistentStruct Saved { get; }
    }

    public class SCPersistenceModule : EntityModuleBase<ISCKernel>, InitializableModule, ClosableModule, ISCPersistence
    {
        public SCPersistenceModule(ISCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(SCPersistenceModule);

        public bool Inited { get; private set; }

        public SCPersistentStruct Saved { get; private set; } = Fallback;
        public static SCPersistentStruct Fallback { get; } = new SCPersistentStruct
        {
            ShowHud = true,
            HudPosX = -0.95f,
            HudPosY = 0.95f,
            VerboseHud = false
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
            SCPersistentStruct persistent = new SCPersistentStruct();
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
            Saved = MyAPIGateway.Utilities.SerializeFromBinary<SCPersistentStruct>(Convert.FromBase64String(storage));
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
    public struct SCPersistentStruct
    {
        [ProtoMember(1)]
        public bool ShowHud;
        [ProtoMember(2)]
        public float HudPosX;
        [ProtoMember(3)]
        public float HudPosY;
        [ProtoMember(4)]
        public bool VerboseHud;
    }
    #endregion
}
