using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager I { get; private set; }

    public NetworkList<ChatMessage> Messages;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 100;
    [SerializeField] private int maxMessageLength = 80;

    private void Awake()
    {
        I = this;
        Messages = new NetworkList<ChatMessage>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (I == this) I = null;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            Messages.OnListChanged += OnMessagesChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
            Messages.OnListChanged -= OnMessagesChanged;
    }

    private void OnMessagesChanged(NetworkListEvent<ChatMessage> changeEvent)
    {
        while (Messages.Count > maxMessages)
            Messages.RemoveAt(0);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendChatRpc(FixedString128Bytes msg, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        string s = msg.ToString().Trim();
        if (string.IsNullOrEmpty(s))
            return;

        if (s.Length > maxMessageLength)
            s = s.Substring(0, maxMessageLength);

        Messages.Add(new ChatMessage
        {
            SenderClientId = sender,
            SenderName = new FixedString32Bytes(ResolveSenderName(sender)),
            Text = new FixedString128Bytes(s)
        });
    }

    public void TrySend(string text)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsClient) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        SendChatRpc(new FixedString128Bytes(text));
    }

    public void SendSystemMessage(string text)
    {
        if (!IsServer) return;

        Messages.Add(new ChatMessage
        {
            SenderClientId = ulong.MaxValue,
            SenderName = default,
            Text = new FixedString128Bytes(text)
        });
    }

    public void ClearMessages()
    {
        if (!IsServer) return;
        Messages.Clear();
    }

    private string ResolveSenderName(ulong senderClientId)
    {
        string resolved = PlayerRegistry.I != null ? PlayerRegistry.I.GetName(senderClientId) : string.Empty;
        if (!string.IsNullOrWhiteSpace(resolved) && !resolved.StartsWith("Player#"))
            return resolved;

        var client = NetworkManager != null && NetworkManager.ConnectedClients.TryGetValue(senderClientId, out var connectedClient)
            ? connectedClient
            : null;

        string fallback = client != null ? client.PlayerObject?.GetComponent<PlayerName>()?.Name.Value.ToString() : string.Empty;
        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

        return resolved;
    }

    private string FilterBadWords(string text)
    {
        return text;
    }
}
