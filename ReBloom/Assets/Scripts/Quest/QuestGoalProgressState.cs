using Unity.Netcode;

public struct QuestGoalProgressState : INetworkSerializable, System.IEquatable<QuestGoalProgressState>
{
    public int goalIndex;
    public int objectId;
    public int currentCount;
    public int targetCount;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref goalIndex);
        serializer.SerializeValue(ref objectId);
        serializer.SerializeValue(ref currentCount);
        serializer.SerializeValue(ref targetCount);
    }

    public bool Equals(QuestGoalProgressState other)
    {
        return goalIndex == other.goalIndex
            && objectId == other.objectId
            && currentCount == other.currentCount
            && targetCount == other.targetCount;
    }
}