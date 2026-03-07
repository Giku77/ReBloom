using Unity.Netcode;

public struct InventorySlotSyncState : INetworkSerializable, System.IEquatable<InventorySlotSyncState>
{
    public int itemID;
    public int count;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemID);
        serializer.SerializeValue(ref count);
    }

    public bool Equals(InventorySlotSyncState other)
    {
        return itemID == other.itemID && count == other.count;
    }
}
