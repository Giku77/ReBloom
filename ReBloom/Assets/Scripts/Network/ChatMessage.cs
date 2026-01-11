using Unity.Netcode;
using Unity.Collections;

public struct ChatMessage : INetworkSerializable, System.IEquatable<ChatMessage>
{
    public ulong SenderClientId;
    public FixedString128Bytes Text;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SenderClientId);
        serializer.SerializeValue(ref Text);
    }

    public bool Equals(ChatMessage other)
    {
        return SenderClientId == other.SenderClientId && Text.Equals(other.Text);
    }
}