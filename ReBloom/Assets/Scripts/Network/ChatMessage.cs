using Unity.Collections;
using Unity.Netcode;

public struct ChatMessage : INetworkSerializable, System.IEquatable<ChatMessage>
{
    public ulong SenderClientId;
    public FixedString32Bytes SenderName;
    public FixedString128Bytes Text;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SenderClientId);
        serializer.SerializeValue(ref SenderName);
        serializer.SerializeValue(ref Text);
    }

    public bool Equals(ChatMessage other)
    {
        return SenderClientId == other.SenderClientId &&
               SenderName.Equals(other.SenderName) &&
               Text.Equals(other.Text);
    }
}
