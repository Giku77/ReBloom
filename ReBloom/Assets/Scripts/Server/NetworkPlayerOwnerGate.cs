using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerOwnerGate : NetworkBehaviour
{
    [Header("Enable only for Owner")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private StageDetector stageDetector;
    [SerializeField] private PlayerStats playerStats;

    [Header("Physics (optional but recommended for 'movement only')")]
    [SerializeField] private Rigidbody rb;

    [Header("Observed State Sync")]
    [SerializeField] private float observedStateSyncInterval = 0.05f;
    [SerializeField] private float cameraPositionThreshold = 0.05f;
    [SerializeField] private float cameraForwardThreshold = 0.01f;

    public static event Action<GameObject> OnLocalPlayerSpawned;
    public static event Action OnLocalPlayerDespawned;

    public readonly NetworkVariable<int> CurrentStageId =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public readonly NetworkVariable<Vector3> CameraPosition =
        new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public readonly NetworkVariable<Vector3> CameraForward =
        new(Vector3.forward, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public readonly NetworkVariable<float> ServerHealth =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<bool> IsDeadState =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<FixedString64Bytes> PersistentPlayerId =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsDeadAuthoritative => IsNetworkedSession ? IsDeadState.Value : playerController != null && playerController.isDead;
    public string PersistentPlayerIdString => PersistentPlayerId.Value.ToString();

    private float nextObservedStateSyncTime;
    private bool hasSyncedObservedDeadState;
    private bool lastObservedDeadState;

    private bool IsNetworkedSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        if (!stageDetector) stageDetector = GetComponent<StageDetector>();
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (Time.unscaledTime < nextObservedStateSyncTime)
            return;

        nextObservedStateSyncTime = Time.unscaledTime + observedStateSyncInterval;
        SyncObservedState();
    }

    public override void OnNetworkSpawn()
    {
        ApplyOwnershipState();

        if (IsServer)
        {
            ServerHealth.Value = playerStats != null ? playerStats.Health.Value : 0f;
            IsDeadState.Value = playerController != null && playerController.isDead;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            OnLocalPlayerDespawned?.Invoke();
    }

    public override void OnGainedOwnership()
    {
        ApplyOwnershipState();
    }

    public override void OnLostOwnership()
    {
        ApplyOwnershipState();
    }

    public void RequestAuthoritativeSelfDamage(float damage)
    {
        if (playerStats == null)
            return;

        if (!IsNetworkedSession)
        {
            playerStats.TakeDamage(damage);
            return;
        }

        if (IsServer)
        {
            ApplyAuthoritativeDamage(damage);
            return;
        }

        RequestSelfDamageRpc(damage);
    }

    public void ApplyAuthoritativeDamage(float damage)
    {
        if (playerStats == null)
            return;

        if (!IsNetworkedSession)
        {
            playerStats.TakeDamage(damage);
            return;
        }

        if (!IsServer)
            return;

        float nextHealth = Mathf.Max(0f, ServerHealth.Value - damage);
        ServerHealth.Value = nextHealth;
        IsDeadState.Value = nextHealth <= 0f;

        if (IsOwner)
            playerStats.SetAuthoritativeHealth(nextHealth, true);
        else
            ApplyDamageClientRpc(nextHealth, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    public void ApplyAuthoritativeStun(float stunDuration)
    {
        if (playerController == null)
            return;

        if (!IsNetworkedSession)
        {
            playerController.ApplyStun(stunDuration);
            return;
        }

        if (!IsServer)
            return;

        if (IsOwner)
            playerController.ApplyStun(stunDuration);
        else
            ApplyStunClientRpc(stunDuration, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }
    public void RequestAuthoritativeResurrection(float health, float hunger, float thirst, float pollution, float temperature)
    {
        if (playerStats == null)
            return;

        if (!IsNetworkedSession)
        {
            ApplyResurrectionStateLocal(health, hunger, thirst, pollution, temperature);
            return;
        }

        if (IsServer)
        {
            ApplyAuthoritativeResurrection(health, hunger, thirst, pollution, temperature);
            return;
        }

        RequestResurrectionRpc(health, hunger, thirst, pollution, temperature);
    }

    public void ApplyRestoredOwnerState(Vector3 position, Vector3 rotationEuler, float health, float hunger, float thirst, float pollution, float temperature, bool isDead, EquipmentSaveDTO equipment)
    {
        if (!IsNetworkedSession || IsOwner)
        {
            ApplyRestoredOwnerStateLocal(position, rotationEuler, health, hunger, thirst, pollution, temperature, isDead, equipment);
            return;
        }

        ApplyRestoredOwnerStateClientRpc(position, rotationEuler, health, hunger, thirst, pollution, temperature, isDead,
            equipment != null ? equipment.clothItemId : 0,
            equipment != null ? equipment.shoesItemId : 0,
            equipment != null ? equipment.toolItemId : 0,
            RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestSelfDamageRpc(float damage)
    {
        ApplyAuthoritativeDamage(damage);
    }
    private void ApplyAuthoritativeResurrection(float health, float hunger, float thirst, float pollution, float temperature)
    {
        if (playerStats == null || !IsServer)
            return;

        float clampedHealth = Mathf.Clamp(health, 0f, playerStats.Health.MaxValue);
        ServerHealth.Value = clampedHealth;
        IsDeadState.Value = false;

        ApplyResurrectionStateLocal(clampedHealth, hunger, thirst, pollution, temperature);

        if (!IsOwner)
            ApplyResurrectionStateClientRpc(clampedHealth, hunger, thirst, pollution, temperature, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestResurrectionRpc(float health, float hunger, float thirst, float pollution, float temperature)
    {
        ApplyAuthoritativeResurrection(health, hunger, thirst, pollution, temperature);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ReportObservedLifeStateRpc(bool isDead)
    {
        IsDeadState.Value = isDead;
        if (isDead && ServerHealth.Value > 0f)
            ServerHealth.Value = 0f;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitPersistentPlayerIdRpc(string persistentId)
    {
        SetPersistentPlayerId(persistentId);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ApplyDamageClientRpc(float newHealth, RpcParams rpcParams = default)
    {
        if (IsServer && IsOwner)
            return;

        playerStats?.SetAuthoritativeHealth(newHealth, true);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ApplyStunClientRpc(float stunDuration, RpcParams rpcParams = default)
    {
        if (IsServer && IsOwner)
            return;

        playerController?.ApplyStun(stunDuration);
    }
    [Rpc(SendTo.SpecifiedInParams)]
    private void ApplyResurrectionStateClientRpc(float health, float hunger, float thirst, float pollution, float temperature, RpcParams rpcParams = default)
    {
        if (IsServer && IsOwner)
            return;

        ApplyResurrectionStateLocal(health, hunger, thirst, pollution, temperature);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ApplyRestoredOwnerStateClientRpc(Vector3 position, Vector3 rotationEuler, float health, float hunger, float thirst, float pollution, float temperature, bool isDead, int clothItemId, int shoesItemId, int toolItemId, RpcParams rpcParams = default)
    {
        if (IsServer && IsOwner)
            return;

        ApplyRestoredOwnerStateLocal(position, rotationEuler, health, hunger, thirst, pollution, temperature, isDead,
            new EquipmentSaveDTO
            {
                clothItemId = clothItemId,
                shoesItemId = shoesItemId,
                toolItemId = toolItemId
            });
    }

    private void SyncObservedState(bool force = false)
    {
        if (!IsOwner)
            return;

        if (stageDetector == null)
            stageDetector = GetComponent<StageDetector>();

        int stageId = stageDetector != null && stageDetector.CurrentStage != null
            ? stageDetector.CurrentStage.StageID
            : 0;

        if (force || CurrentStageId.Value != stageId)
            CurrentStageId.Value = stageId;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            Vector3 camForward = cam.transform.forward;

            if (force || Vector3.Distance(CameraPosition.Value, camPos) >= cameraPositionThreshold)
                CameraPosition.Value = camPos;

            if (force || Vector3.Distance(CameraForward.Value, camForward) >= cameraForwardThreshold)
                CameraForward.Value = camForward;
        }

        bool observedDead = playerController != null && playerController.isDead;
        if (force || !hasSyncedObservedDeadState || observedDead != lastObservedDeadState)
        {
            hasSyncedObservedDeadState = true;
            lastObservedDeadState = observedDead;

            if (IsServer)
                IsDeadState.Value = observedDead;
            else
                ReportObservedLifeStateRpc(observedDead);
        }
    }

    private void RegisterPersistentPlayerId()
    {
        string persistentId = ResolvePersistentPlayerId();
        if (string.IsNullOrWhiteSpace(persistentId))
            return;

        if (IsServer)
            SetPersistentPlayerId(persistentId);
        else
            SubmitPersistentPlayerIdRpc(persistentId);
    }

    private void SetPersistentPlayerId(string persistentId)
    {
        persistentId = persistentId?.Trim();
        if (string.IsNullOrWhiteSpace(persistentId))
            return;

        PersistentPlayerId.Value = new FixedString64Bytes(persistentId);
        PlayerRegistry.I?.TryApplySavedStateToPlayer(this);
    }

    private string ResolvePersistentPlayerId()
    {
        if (!string.IsNullOrWhiteSpace(PlayFabAuth.CurrentPlayFabId))
            return PlayFabAuth.CurrentPlayFabId;

        return SystemInfo.deviceUniqueIdentifier;
    }

    private void ApplyOwnershipState()
    {
        bool isLocal = IsOwner;
        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL;

        Debug.Log($"[Gate] apply name={name} netId={NetworkObjectId} owner={OwnerClientId} local={localClientId} IsOwner={IsOwner} IsLocalPlayer={IsLocalPlayer} IsServer={IsServer} IsClient={IsClient}");

        if (playerController != null)
            playerController.enabled = isLocal;

        if (rb != null)
        {
            rb.isKinematic = !isLocal;
            if (isLocal)
                rb.WakeUp();
        }

        if (playerInput != null)
        {
            if (!isLocal)
            {
                playerInput.DeactivateInput();
                playerInput.enabled = false;
            }
            else
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();

                var playerMap = playerInput.actions != null
                    ? playerInput.actions.FindActionMap("Player", false)
                    : null;

                if (playerMap != null)
                {
                    playerMap.Enable();
                    if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != playerMap.name)
                        playerInput.SwitchCurrentActionMap(playerMap.name);
                }
                else
                {
                    playerInput.currentActionMap?.Enable();
                }
            }
        }

        if (!isLocal)
            return;

        bool shouldBlock = UIManager.Instance != null && UIManager.Instance.IsBlockedInput;
        playerController?.SetBlocked(shouldBlock);
        RegisterPersistentPlayerId();
        SyncObservedState(force: true);
        CameraRig.I?.Follow(transform);
        UIRoot.I?.BindLocalPlayer(playerController);
        OnLocalPlayerSpawned?.Invoke(gameObject);
    }

    private void ApplyRestoredOwnerStateLocal(Vector3 position, Vector3 rotationEuler, float health, float hunger, float thirst, float pollution, float temperature, bool isDead, EquipmentSaveDTO equipment)
    {
        transform.SetPositionAndRotation(position, Quaternion.Euler(rotationEuler));
        playerStats?.ApplyRestoredState(health, hunger, thirst, pollution, temperature, isDead);

        if (playerController != null)
        {
            playerController.isDead = isDead;
            playerController.SetBlocked(isDead);
        }

        ApplyEquipmentLocal(equipment);
        CameraRig.I?.Follow(transform);
    }
    private void ApplyResurrectionStateLocal(float health, float hunger, float thirst, float pollution, float temperature)
    {
        playerStats?.ApplyResurrectionState(health, hunger, thirst, pollution, temperature);

        if (playerController != null)
        {
            playerController.isDead = false;
            playerController.SetBlocked(false);
        }
    }

    private void ApplyEquipmentLocal(EquipmentSaveDTO equipment)
    {
        var equipManager = GetComponent<PlayerEquipManager>();
        if (equipManager == null)
            return;

        equipManager.ClearAllEquipData();
        if (equipment == null)
            return;

        if (equipment.clothItemId > 0)
        {
            var cloth = ItemDatabase.I?.GetItem(equipment.clothItemId) as ProtectiveItemData;
            if (cloth != null)
                equipManager.Apply(cloth);
        }

        if (equipment.shoesItemId > 0)
        {
            var shoes = ItemDatabase.I?.GetItem(equipment.shoesItemId) as ProtectiveItemData;
            if (shoes != null)
                equipManager.Apply(shoes);
        }

        if (equipment.toolItemId > 0)
        {
            var tool = ItemDatabase.I?.GetItem(equipment.toolItemId) as ToolItemData;
            if (tool != null)
                equipManager.Apply(tool);
        }
    }
}






