using Unity.Netcode;
using Unity.Collections;

public struct PlayerEntry : INetworkSerializable, System.IEquatable<PlayerEntry>
{
    public ulong ClientId;
    public FixedString32Bytes Name;

    public bool Equals(PlayerEntry other) => ClientId == other.ClientId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
    }
}
