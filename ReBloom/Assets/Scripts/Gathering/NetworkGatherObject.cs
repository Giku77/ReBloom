using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkGatherObject : NetworkBehaviour
{
    [SerializeField] private GatherObject gatherObject;

    private NetworkVariable<double> nextAvailableServerTime = new(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> permanentlyDisabled = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (gatherObject == null) gatherObject = GetComponent<GatherObject>();
    }

    private void OnNextTimeChanged(double prev, double cur) => gatherObject?.RefreshFromNetwork();
    private void OnPermanentChanged(bool prev, bool cur) => gatherObject?.RefreshFromNetwork();

    public override void OnNetworkSpawn()
    {
        nextAvailableServerTime.OnValueChanged += OnNextTimeChanged;
        permanentlyDisabled.OnValueChanged += OnPermanentChanged;

        if (IsServer)
        {
            double now = NetworkManager.Singleton.ServerTime.Time;
            if (nextAvailableServerTime.Value <= 0d)
                nextAvailableServerTime.Value = now; // 처음엔 바로 사용 가능
        }

        gatherObject?.RefreshFromNetwork();
    }

    public override void OnNetworkDespawn()
    {
        nextAvailableServerTime.OnValueChanged -= OnNextTimeChanged;
        permanentlyDisabled.OnValueChanged -= OnPermanentChanged;
    }

    // ---------- 클라에서 읽기용 ----------
    public bool IsAvailableNow()
    {
        if (permanentlyDisabled.Value) return false;
        if (NetworkManager.Singleton == null) return true;

        double now = NetworkManager.Singleton.ServerTime.Time;
        return now >= nextAvailableServerTime.Value;
    }

    public float GetCooldownRemaining()
    {
        if (NetworkManager.Singleton == null) return 0f;
        double now = NetworkManager.Singleton.ServerTime.Time;
        return Mathf.Max(0f, (float)(nextAvailableServerTime.Value - now));
    }

    // ---------- 채집 요청 ----------
    public void TryRequestGather(PlayerController player)
    {
        if (player == null) return;
        RequestGatherRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestGatherRpc(RpcParams rpcParams = default)
    {
        if (!IsServer || !IsSpawned) return;

        if (permanentlyDisabled.Value) return;

        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            return;

        float dist = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
        if (dist > 3f) return;

        double now = NetworkManager.Singleton.ServerTime.Time;
        if (now < nextAvailableServerTime.Value) return;

        var gm = FindAnyObjectByType<GatherManager>();
        if (gm == null) return;

        bool isNight = false;
        if (DayNightCycle.Instance != null) isNight = DayNightCycle.Instance.IsNightTime();

        var drops = gm.GetDropResult(gatherObject.gatherObjectID, isNight);
        if (drops == null || drops.item == null) return;

        var inv = client.PlayerObject.GetComponent<PlayerInventoryRuntime>();
        if (inv == null) return;

        int added = inv.AddItemFromWorld(drops.item.itemID, drops.amount);
        int overflow = drops.amount - added;

        if (added <= 0) return;

        NetworkQuestManager.I?.AddInteractProgressServer(gatherObject.gatherObjectID, 1);
        NetworkQuestManager.I?.AddCollectProgressServer(drops.item.itemID, added);

        ShowGatherFeedbackRpc(drops.item.itemID, added, overflow, RpcTarget.Single(clientId, RpcTargetUse.Temp));

        float respawn = gatherObject.GetRespawnSecondsSafe(); 
        if (respawn < 0.1f) respawn = 0.1f;

        nextAvailableServerTime.Value = now + respawn;

        if (gatherObject.isDestroyObject)
        {
            permanentlyDisabled.Value = true;
            NetworkObject.Despawn(true);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowGatherFeedbackRpc(int itemId, int added, int overflow, RpcParams rpcParams = default)
    {
        var invUI = FindFirstObjectByType<GameInventory>();
        if (invUI == null) return;

        invUI.NotifyPickupFeedback(itemId, added, overflow);
    }

    public void ServerDisablePermanently()
    {
        if (!IsServer) return;
        permanentlyDisabled.Value = true;
        if (IsSpawned) NetworkObject.Despawn(true);
    }
}