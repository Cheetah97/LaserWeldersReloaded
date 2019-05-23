using System;
using EemRdx.Networking;

namespace EemRdx.SessionModules
{
    public interface INetworker : ISessionModule
    {
        bool Inited { get; }
        uint ModID { get; }

        bool RegisterHandler(string DataTag, Action<NetworkerMessage> handler);
        void SendTo(ulong ID, string SenderName, string DataDescription, byte[] Data);
        void SendToAll(string SenderName, string DataDescription, byte[] Data);
        void SendToServer(string SenderName, string DataDescription, byte[] Data);
        bool UnregisterHandler(string SenderName, Action<NetworkerMessage> handler);
    }
}