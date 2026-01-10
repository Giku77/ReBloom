using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerRegistry : NetworkBehaviour
{
    public static PlayerRegistry I { get; private set; }

    public NetworkVariable<FixedString32Bytes> JoinCode =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<PlayerEntry> Players;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        Players = new NetworkList<PlayerEntry>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        var code = JoinCodeStore.Current;
        if (!string.IsNullOrEmpty(code))
            JoinCode.Value = new FixedString32Bytes(code);

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Remove(clientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitNameRpc(ulong clientId, string name)
    {
        if (!IsServer) return;

        var fixedName = Sanitize(name);
        Upsert(clientId, fixedName);
    }

    private void Upsert(ulong clientId, FixedString32Bytes name)
    {
        var entry = new PlayerEntry { ClientId = clientId, Name = name };

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Players[i] = entry;
                return;
            }
        }
        Players.Add(entry);
    }

    private void Remove(ulong clientId)
    {
        for (int i = Players.Count - 1; i >= 0; i--)
        {
            if (Players[i].ClientId == clientId)
                Players.RemoveAt(i);
        }
    }

    private FixedString32Bytes Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) s = "Player";
        s = s.Trim();
        if (s.Length > 16) s = s.Substring(0, 16);
        return new FixedString32Bytes(s);
    }
}
