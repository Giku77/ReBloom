using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

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
        
        // 서버에서 메시지 개수 관리
        if (IsServer)
        {
            Messages.OnListChanged += OnMessagesChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsServer)
        {
            Messages.OnListChanged -= OnMessagesChanged;
        }
    }

    private void OnMessagesChanged(NetworkListEvent<ChatMessage> changeEvent)
    {
        // 메시지가 너무 많으면 오래된 것부터 삭제
        while (Messages.Count > maxMessages)
        {
            Messages.RemoveAt(0);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendChatRpc(FixedString128Bytes msg, RpcParams rpcParams = default)
    {
        // 누가 보냈는지 서버에서 확정
        ulong sender = rpcParams.Receive.SenderClientId;

        // 간단 검증
        var s = msg.ToString().Trim();
        if (string.IsNullOrEmpty(s)) return;
        if (s.Length > maxMessageLength) 
            s = s.Substring(0, maxMessageLength);

        // 금칙어 필터링 (필요시 추가)
        // s = FilterBadWords(s);

        Messages.Add(new ChatMessage
        {
            SenderClientId = sender,
            Text = new FixedString128Bytes(s)
        });
    }

    /// <summary>
    /// 클라이언트에서 호출용 래퍼
    /// </summary>
    public void TrySend(string text)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsClient) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        SendChatRpc(new FixedString128Bytes(text));
    }

    /// <summary>
    /// 시스템 메시지 전송 (서버 전용)
    /// </summary>
    public void SendSystemMessage(string text)
    {
        if (!IsServer) return;

        Messages.Add(new ChatMessage
        {
            SenderClientId = ulong.MaxValue, // 시스템 메시지 표시용
            Text = new FixedString128Bytes(text)
        });
    }

    /// <summary>
    /// 채팅 내역 초기화 (서버 전용)
    /// </summary>
    public void ClearMessages()
    {
        if (!IsServer) return;
        Messages.Clear();
    }

    // 필요시 금칙어 필터 구현
    private string FilterBadWords(string text)
    {
        // TODO: 금칙어 리스트로 필터링
        return text;
    }
}