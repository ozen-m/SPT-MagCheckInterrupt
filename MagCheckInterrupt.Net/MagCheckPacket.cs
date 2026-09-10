using Fika.Core.Networking.LiteNetLib.Utils;

namespace MagCheckInterrupt.Net;

public struct MagCheckPacket(int netId, MagCheckPacket.CheckPacketType type) : INetSerializable
{
    public int NetId = netId;
    public CheckPacketType Type = type;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Type = reader.GetEnum<CheckPacketType>();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutEnum(Type);
    }

    public enum CheckPacketType : byte
    {
        ReloadCalled,
        RestoreSpeed,
    }
}
