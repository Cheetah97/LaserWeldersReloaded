using EemRdx.SessionModules;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;

namespace EemRdx.Networking
{
    public class Sync<T> : IRegistrableSync<T>
    {
        public T Data { get; protected set; }
        public string DataDescription { get; protected set; }
        public bool UseServersideSync { get; protected set; }
        protected string SenderName;
        protected HashSet<Action> EventHandlers = new HashSet<Action>();
        protected INetworker Networker;
        protected ILogProvider Log;
        public static implicit operator T(Sync<T> Object)
        {
            return Object.Data;
        }

        /// <summary>
        /// Don't forget to Ask() to get actual value from server after registration.
        /// </summary>
        public Sync(INetworker Networker, string DataDescription, T DefaultValue = default(T), bool UseServersideSync = true)
        {
            this.Networker = Networker;
            this.DataDescription = DataDescription;
            this.UseServersideSync = UseServersideSync;
            SenderName = $"GenericSyncer {DataDescription}";
            Data = DefaultValue;
            Log = (Networker as NetworkerModule).MySessionKernel.Log;
            Log.WriteToLog("Sync.ctor()", $"Syncer created: {SenderName}", LoggingLevelEnum.DebugLog);
        }

        /// <summary>
        /// Registers a handler. Don't forget to initialize Networker beforehand.
        /// </summary>
        public void Register()
        {
            Networker.RegisterHandler(SenderName, Handler);
        }

        /// <summary>
        /// Unregisters a handler.
        /// </summary>
        public void Unregister()
        {
            Networker.UnregisterHandler(SenderName, Handler);
        }

        public void RegisterEventHandler(Action EventHandler)
        {
            EventHandlers.Add(EventHandler);
        }

        private void InvokeEventHandlers()
        {
            foreach (Action EventHandler in EventHandlers)
            {
                EventHandler();
            }
        }

        protected void Handler(NetworkerMessage message)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (message.DataDescription == "UpdateRequest")
                {
                    Data = Deserialize(message.Data);
                    Networker.SendToAll(SenderName, "Update", Serialize(Data));
                    InvokeEventHandlers();
                }
                else if (message.DataDescription == "Get")
                {
                    Networker.SendTo(message.SenderID, SenderName, "Update", Serialize(Data));
                }
            }
            else
            {
                if (message.DataDescription == "Update" && message.SenderID == MyAPIGateway.Multiplayer.ServerId)
                {
                    Data = Deserialize(message.Data);
                    InvokeEventHandlers();
                }
            }
        }

        protected static byte[] Serialize(T Object)
        {
            return MyAPIGateway.Utilities.SerializeToBinary(Object);
        }

        protected static T Deserialize(byte[] Raw)
        {
            return MyAPIGateway.Utilities.SerializeFromBinary<T>(Raw);
        }

        /// <summary>
        /// Updates a variable or sends a request to server (if called clientside).
        /// </summary>
        public void Set(T New)
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    Networker.SendToAll(SenderName, "Update", Serialize(New));
                }
                else
                {
                    Networker.SendToServer(SenderName, "UpdateRequest", Serialize(New));
                }
            }
            
            if (MyAPIGateway.Multiplayer.IsServer || !UseServersideSync)
            {
                Data = New;
                InvokeEventHandlers();
            }
        }

        /// <summary>
        /// (Server only) Sends the data to all clients
        /// </summary>
        public void Update()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                Networker.SendToAll(SenderName, "Update", Serialize(Data));
            }
        }

        /// <summary>
        /// Asks the server for actual value.
        /// </summary>
        public void Ask()
        {
            Networker.SendToServer(SenderName, "Get", null);
        }
    }

    public class EntitySync<T> : Sync<T>
    {
        public EntitySync(INetworker Networker, VRage.ModAPI.IMyEntity Entity, string DataDescription) : base(Networker, DataDescription)
        {
            SenderName = $"EntitySyncer {DataDescription} for {Entity.EntityId}";
        }
    }
}
