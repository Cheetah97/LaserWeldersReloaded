using System;
using System.Collections.Generic;
using EemRdx.Networking;
using Sandbox.ModAPI;

namespace EemRdx.SessionModules
{
    public class NetworkerModule : SessionModuleBase<ISessionKernel>, InitializableModule, UnloadableModule, INetworker
    {
        public NetworkerModule(ISessionKernel MySessionKernel) : base(MySessionKernel) { }

        public const ushort CommChannel = 7207;
        public bool Inited { get; protected set; }
        // We won't go beyond 4KKK of workshop creations quickly, right?
        public uint ModID { get; protected set; }

        public override string DebugModuleName => "NetworkerModule";

        private static Dictionary<string, HashSet<Action<NetworkerMessage>>> MessageHandlers = new Dictionary<string, HashSet<Action<NetworkerMessage>>>();

        public void Init()
        {
            if (Inited) return;
            ModID = MySessionKernel.ModID;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(CommChannel, Handler);
            Inited = true;
        }

        void Handler(byte[] rawmessage)
        {
            NetworkerMessage message = MyAPIGateway.Utilities.SerializeFromBinary<NetworkerMessage>(rawmessage);
            if (message == null || message.ModID != ModID) return;
            // TODO: <Cheetah Comment> Add logging
            if (MessageHandlers.ContainsKey(message.DataTag))
            {
                foreach (Action<NetworkerMessage> handler in MessageHandlers[message.DataTag])
                {
                    try
                    {
                        handler?.Invoke(message);
                    }
                    catch { }
                }
            }
        }

        public bool RegisterHandler(string DataTag, Action<NetworkerMessage> handler)
        {
            if (handler == null) return false;
            if (MessageHandlers.ContainsKey(DataTag)) return MessageHandlers[DataTag].Add(handler);
            else
            {
                MessageHandlers.Add(DataTag, new HashSet<Action<NetworkerMessage>> { handler });
                return true;
            }
        }

        public bool UnregisterHandler(string SenderName, Action<NetworkerMessage> handler)
        {
            if (handler == null) return false;
            if (!MessageHandlers.ContainsKey(SenderName)) return false;
            else return MessageHandlers[SenderName].Remove(handler);
        }

        private byte[] GenerateMessage(string senderName, string dataDescription, byte[] data)
        {
            NetworkerMessage message = new NetworkerMessage
            {
                ModID = ModID,
                SenderID = MyAPIGateway.Multiplayer.MyId,
                DataTag = senderName,
                DataDescription = dataDescription,
                Data = data
            };
            return MyAPIGateway.Utilities.SerializeToBinary(message);
        }

        public void SendToAll(string SenderName, string DataDescription, byte[] Data)
        {
            byte[] Raw = GenerateMessage(SenderName, DataDescription, Data);
            MyAPIGateway.Multiplayer.SendMessageToOthers(CommChannel, Raw);
        }

        public void SendTo(ulong ID, string SenderName, string DataDescription, byte[] Data)
        {
            byte[] Raw = GenerateMessage(SenderName, DataDescription, Data);
            MyAPIGateway.Multiplayer.SendMessageTo(CommChannel, Raw, ID);
        }

        public void SendToServer(string SenderName, string DataDescription, byte[] Data)
        {
            byte[] Raw = GenerateMessage(SenderName, DataDescription, Data);
            MyAPIGateway.Multiplayer.SendMessageToServer(CommChannel, Raw);
        }

        public void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(CommChannel, Handler);
        }
    }
}
