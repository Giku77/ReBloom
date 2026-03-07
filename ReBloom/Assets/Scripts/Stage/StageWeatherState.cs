using Unity.Netcode;

public struct StageWeatherState : INetworkSerializable, System.IEquatable<StageWeatherState>
{
    public int stageId;
    public WeatherType weather;

    public float duration;
    public double startServerTime; // NetworkManager.ServerTime.Time 기준

    public float pollution;
    public float thirst;
    public float temp;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref stageId);
        serializer.SerializeValue(ref weather);
        serializer.SerializeValue(ref duration);
        serializer.SerializeValue(ref startServerTime);
        serializer.SerializeValue(ref pollution);
        serializer.SerializeValue(ref thirst);
        serializer.SerializeValue(ref temp);
    }

    public bool Equals(StageWeatherState other) => stageId == other.stageId;
}