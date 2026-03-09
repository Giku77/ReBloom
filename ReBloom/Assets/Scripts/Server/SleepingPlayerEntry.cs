using Unity.Netcode;

public struct SleepingPlayerEntry : INetworkSerializable, System.IEquatable<SleepingPlayerEntry>
{
    public ulong ClientId;

    public bool Equals(SleepingPlayerEntry other) => ClientId == other.ClientId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
    }
}
