using ProtoBuf;

namespace EemRdx.Networking
{
    [ProtoContract]
    public class NetworkerMessage
    {
        [ProtoMember]
        public uint ModID;
        /// <summary>
        /// As in MyAPIGateway.Multiplayer.MyId
        /// </summary>
        [ProtoMember]
        public ulong SenderID;
        /// <summary>
        /// Describes the type of sender object. You may put the name of your class here. It is used to filter whom to bother with data.
        /// </summary>
        [ProtoMember]
        public string DataTag;
        /// <summary>
        /// Can be tossed to message receiver in order to give it an idea how to deserialize given data.
        /// </summary>
        [ProtoMember]
        public string DataDescription;
        [ProtoMember]
        public byte[] Data;
    }
}
