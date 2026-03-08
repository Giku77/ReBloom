using Cysharp.Threading.Tasks;
using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkGatherObject : NetworkBehaviour
{
    private const float PowerBoxSequenceDestroyDelay = 1.5f;

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

    private void OnPermanentChanged(bool prev, bool cur)
    {
        if (cur)
        {
            if (!IsServer)
                HideGatherObjectLocally();
            return;
        }

        gatherObject?.RefreshFromNetwork();
    }

    public override void OnNetworkSpawn()
    {
        nextAvailableServerTime.OnValueChanged += OnNextTimeChanged;
        permanentlyDisabled.OnValueChanged += OnPermanentChanged;

        if (IsServer)
        {
            double now = NetworkManager.Singleton.ServerTime.Time;
            if (nextAvailableServerTime.Value <= 0d)
                nextAvailableServerTime.Value = now;
        }

        gatherObject?.RefreshFromNetwork();
    }

    public override void OnNetworkDespawn()
    {
        nextAvailableServerTime.OnValueChanged -= OnNextTimeChanged;
        permanentlyDisabled.OnValueChanged -= OnPermanentChanged;

        if (permanentlyDisabled.Value)
            HideGatherObjectLocally();
    }

    private void HideGatherObjectLocally()
    {
        if (gatherObject == null)
            return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsServer)
        {
            DestroyedObjectRegistry.I?.MarkDestroyed(gatherObject.SaveKey);
        }

        if (gatherObject.TryGetComponent<InteractionHighlight>(out var highlight))
        {
            highlight.Hide();

            if (highlight.TryGetComponent<OutlineToggle>(out var outline))
                outline.SetOutlined(false);
        }

        gatherObject.gameObject.SetActive(false);
    }

    private void FinalizeDestroyedObjectOnServer()
    {
        if (!IsServer || gatherObject == null)
            return;

        Debug.Log($"[NetworkGatherObject] Finalize destroy id={gatherObject.gatherObjectID} name={gatherObject.name}");
        Destroy(gatherObject.gameObject);
    }

    private bool TryHandleSpecialDestroyResult(ulong interactingClientId)
    {
        if (gatherObject == null || !gatherObject.IsPowerBoxFenceConfigured)
            return false;

        ulong hostClientId = NetworkManager.ServerClientId;

        if (interactingClientId == hostClientId)
        {
            gatherObject.PlayPowerBoxFenceSequenceLocal();
        }
        else
        {
            gatherObject.DisableLinkedFenceLocal();
            PlayPowerBoxFenceSequenceRpc(RpcTarget.Single(interactingClientId, RpcTargetUse.Temp));
        }

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == hostClientId || clientId == interactingClientId)
                continue;

            DisableLinkedFenceRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        return true;
    }

    private async UniTaskVoid FinalizeDestroyedObjectAfterDelay(float delaySeconds)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), ignoreTimeScale: true, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!this || !IsServer)
            return;

        if (IsSpawned)
            NetworkObject.Despawn(false);

        FinalizeDestroyedObjectOnServer();
    }

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

        var controller = client.PlayerObject.GetComponent<PlayerController>();
        if (controller == null)
            return;

        float dist = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
        if (dist > 3f) return;

        if (gatherObject != null && !gatherObject.CanInteract(controller))
            return;

        double now = NetworkManager.Singleton.ServerTime.Time;
        if (now < nextAvailableServerTime.Value) return;

        var gm = FindAnyObjectByType<GatherManager>();
        if (gm == null) return;

        bool isNight = false;
        if (DayNightCycle.Instance != null) isNight = DayNightCycle.Instance.IsNightTime();

        var drops = gm.GetDropResult(gatherObject.gatherObjectID, isNight);

        int added = 0;
        int overflow = 0;
        int itemId = 0;

        if (drops != null && drops.item != null)
        {
            var inv = client.PlayerObject.GetComponent<PlayerInventoryRuntime>();
            if (inv == null) return;

            itemId = drops.item.itemID;
            added = inv.AddItemFromWorld(itemId, drops.amount);
            overflow = drops.amount - added;

            if (added <= 0)
            {
                Debug.Log($"[NetworkGatherObject] Gather reward could not be added id={gatherObject.gatherObjectID} player={clientId}");
                return;
            }

            NetworkQuestManager.I?.AddCollectProgressServer(itemId, added);
            ShowGatherFeedbackRpc(itemId, added, overflow, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
        else
        {
            Debug.Log($"[NetworkGatherObject] No drop configured for gatherObjectID={gatherObject.gatherObjectID}. Proceeding with interaction-only result.");
        }

        NetworkQuestManager.I?.AddInteractProgressServer(gatherObject.gatherObjectID, 1);

        float respawn = gatherObject.GetRespawnSecondsSafe();
        if (respawn < 0.1f) respawn = 0.1f;

        nextAvailableServerTime.Value = now + respawn;

        if (gatherObject.isDestroyObject)
        {
            Debug.Log($"[NetworkGatherObject] Destroy path hit id={gatherObject.gatherObjectID} added={added}");
            bool delayedDestroy = TryHandleSpecialDestroyResult(clientId);
            permanentlyDisabled.Value = true;
            HideGatherObjectLocally();

            if (delayedDestroy)
            {
                FinalizeDestroyedObjectAfterDelay(PowerBoxSequenceDestroyDelay);
                return;
            }

            NetworkObject.Despawn(false);
            FinalizeDestroyedObjectOnServer();
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowGatherFeedbackRpc(int itemId, int added, int overflow, RpcParams rpcParams = default)
    {
        var invUI = FindFirstObjectByType<GameInventory>();
        if (invUI == null) return;

        invUI.NotifyPickupFeedback(itemId, added, overflow);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void PlayPowerBoxFenceSequenceRpc(RpcParams rpcParams = default)
    {
        gatherObject?.PlayPowerBoxFenceSequenceLocal();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void DisableLinkedFenceRpc(RpcParams rpcParams = default)
    {
        gatherObject?.DisableLinkedFenceLocal();
    }

    public void ServerDisablePermanently()
    {
        if (!IsServer) return;
        permanentlyDisabled.Value = true;
        HideGatherObjectLocally();
        if (IsSpawned) NetworkObject.Despawn(false);
        FinalizeDestroyedObjectOnServer();
    }
}
