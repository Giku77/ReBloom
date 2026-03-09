using Unity.Netcode;

public struct GreenhouseUpgradeProgressState : INetworkSerializable, System.IEquatable<GreenhouseUpgradeProgressState>
{
    public int sort;
    public int completedGrade;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref sort);
        serializer.SerializeValue(ref completedGrade);
    }

    public bool Equals(GreenhouseUpgradeProgressState other)
    {
        return sort == other.sort && completedGrade == other.completedGrade;
    }
}
