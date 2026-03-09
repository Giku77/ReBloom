using Unity.Netcode;

public struct FarmSlotNetworkState : INetworkSerializable, System.IEquatable<FarmSlotNetworkState>
{
    public int state;
    public int cropId;
    public int stageIndex;
    public float stageTimer;
    public int wateredCount;
    public float fertilizerRemain;
    public float growSpeedMultiplier;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref state);
        serializer.SerializeValue(ref cropId);
        serializer.SerializeValue(ref stageIndex);
        serializer.SerializeValue(ref stageTimer);
        serializer.SerializeValue(ref wateredCount);
        serializer.SerializeValue(ref fertilizerRemain);
        serializer.SerializeValue(ref growSpeedMultiplier);
    }

    public bool Equals(FarmSlotNetworkState other)
    {
        return state == other.state
            && cropId == other.cropId
            && stageIndex == other.stageIndex
            && stageTimer.Equals(other.stageTimer)
            && wateredCount == other.wateredCount
            && fertilizerRemain.Equals(other.fertilizerRemain)
            && growSpeedMultiplier.Equals(other.growSpeedMultiplier);
    }
}
