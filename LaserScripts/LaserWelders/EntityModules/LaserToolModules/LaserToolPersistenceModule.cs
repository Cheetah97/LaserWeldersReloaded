using EemRdx.EntityModules;
using ProtoBuf;
using Sandbox.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface ILaserToolPersistence : IPersistence<LaserToolPersistentStruct> { }

    public class LaserToolPersistenceModule : PersistenceModuleBase<ILaserWeldersSessionKernel, IMyShipWelder, ILaserToolKernel, LaserToolPersistentStruct, LaserToolPersistenceModule, LaserTerminalControlsModule>, ILaserToolPersistence
    {
        public LaserToolPersistenceModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(LaserToolPersistenceModule);

        public override LaserToolPersistentStruct Default { get; } = new LaserToolPersistentStruct
        {
            BeamLength = 4,
            DistanceBased = true,
            SpeedMultiplier = 1,
            ToolMode = true,
        };
    }

    //public interface ILaserToolPersistence : IEntityModule
    //{
    //    LaserToolPersistentStruct Saved { get; }
    //}

    //public class LaserToolPersistenceModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, ClosableModule, ILaserToolPersistence
    //{
    //    public LaserToolPersistenceModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

    //    public override string DebugModuleName { get; } = nameof(LaserToolPersistenceModule);

    //    public bool Inited { get; private set; }

    //    public LaserToolPersistentStruct Saved { get; private set; } = Fallback;
    //    public static LaserToolPersistentStruct Fallback { get; } = new LaserToolPersistentStruct
    //    {
    //        BeamLength = 4,
    //        DistanceBased = true,
    //        SpeedMultiplier = 1,
    //        ToolMode = true,
    //    };

    //    void InitializableModule.Init()
    //    {
    //        Inited = true;
    //        WriteToLog("Load", $"\r\nStarting persistence init");
    //        EnsureSavingComponent();
    //        Load();
    //        MyKernel.SessionBase.SaveProvider.Subscribe(Save);
    //        WriteToLog("Load", $"\r\nPersistence init done");
    //    }

    //    void ClosableModule.Close()
    //    {
    //        MyKernel.SessionBase.SaveProvider?.Unsubscribe(Save);
    //    }

    //    private void Save()
    //    {
    //        if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
    //            return;
    //        LaserToolPersistentStruct persistent = MyKernel.TermControls.CurrentSettings;
    //        string Raw = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(persistent));
    //        MyKernel.Block.Storage.SetValue(MyKernel.SessionBase.StorageGuid, Raw);
    //        WriteToLog("Save", $"Storage: {Raw}");
    //    }

    //    private void Load()
    //    {
    //        if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer) return;

    //        string storage = MyKernel.Block.Storage[MyKernel.SessionBase.StorageGuid];
    //        if (string.IsNullOrWhiteSpace(storage))
    //        {
    //            WriteToLog("Load", $"Storage.Guid is empty");
    //            return;
    //        }
    //        WriteToLog("Load", $"Storage: {storage}");
    //        Saved = MyAPIGateway.Utilities.SerializeFromBinary<LaserToolPersistentStruct>(Convert.FromBase64String(storage));
    //    }

    //    private void EnsureSavingComponent()
    //    {
    //        if (MyKernel.Block.Storage?.ContainsKey(MyKernel.SessionBase.StorageGuid) != true)
    //        {
    //            if (MyKernel.Block.Storage == null)
    //            {
    //                MyKernel.Block.Storage = new MyModStorageComponent();
    //                WriteToLog("EnsureSavingComponent", $"Storage is null, creating new Storage");
    //            }
    //            if (!MyKernel.Block.Storage.ContainsKey(MyKernel.SessionBase.StorageGuid))
    //            {
    //                MyKernel.Block.Storage.Add(MyKernel.SessionBase.StorageGuid, string.Empty);
    //                WriteToLog("EnsureSavingComponent", $"Storage is empty, adding Guid to Storage");
    //            }
    //        }
    //    }
    //}

    #region Struct
    [ProtoContract]
    public struct LaserToolPersistentStruct : IPersistentStruct
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
