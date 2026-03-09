using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GreenhouseContext : NetworkBehaviour, ISaveable
{
    [Serializable]
    public class ActivatableEntry
    {
        public string key;
        public GameObject target;
    }

    [Header("Identity")]
    [SerializeField] private string greenhouseInstanceId;

    [Header("Activatables (Key -> GameObject)")]
    [SerializeField] private List<ActivatableEntry> activatables = new();

    private readonly Dictionary<string, List<GameObject>> activatableMap = new();
    private readonly Dictionary<GameObject, bool> defaultActiveStates = new();
    private readonly GreenhouseUpgradeState runtimeState = new();

    private NetworkList<GreenhouseUpgradeProgressState> syncedProgress = new();
    private GreenhouseUpgradeDB upgradeDB;
    private GreenhouseSprinklerSystem sprinklerSystem;
    private GreenhouseFarmDroneSystem droneSystem;
    private bool defaultSprinklerActive;
    private bool defaultDroneActive;
    private bool defaultDroneAutoFertilize;

    public string Id => ResolveRuntimeId();
    public string EntityGuid => ResolveRuntimeId();

    public event Action OnUpgradeStateChanged;

    private bool IsNetworkSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {        EnsureId();
        runtimeState.greenhouseId = ResolveRuntimeId();
        RebuildMap();

        EnsureUpgradeDbLoaded();
        sprinklerSystem = GetComponentInChildren<GreenhouseSprinklerSystem>(true);
        droneSystem = GetComponentInChildren<GreenhouseFarmDroneSystem>(true);
        defaultSprinklerActive = sprinklerSystem != null && sprinklerSystem.gameObject.activeSelf;
        defaultDroneActive = droneSystem != null && droneSystem.gameObject.activeSelf;
        defaultDroneAutoFertilize = droneSystem != null && droneSystem.AutoFertilize;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        syncedProgress.OnListChanged += HandleProgressChanged;

        if (IsServer)
            SyncRuntimeStateToNetwork();
        else
            ApplySyncedStateToRuntimeState();
    }

    public override void OnNetworkDespawn()
    {
        syncedProgress.OnListChanged -= HandleProgressChanged;
        base.OnNetworkDespawn();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EnsureId();
            RebuildMap();
        }
    }
#endif

    public GreenhouseUpgradeState GetRuntimeStateSnapshot()
    {
        string currentId = ResolveRuntimeId();
        var copy = new GreenhouseUpgradeState { greenhouseId = currentId };
        for (int i = 0; i < runtimeState.progress.Count; i++)
            copy.progress.Add(runtimeState.progress[i]);
        return copy;
    }

    public bool RequestPurchaseFromLocalPlayer(int upgradeId)
    {
        EnsureUpgradeDbLoaded();
        if (upgradeDB == null || !upgradeDB.TryGet(upgradeId, out var row))
            return false;

        var localInventory = FindLocalInventory();
        if (localInventory == null)
            return false;

        if (!GreenhouseUpgradeService.CanPurchase(GetRuntimeStateSnapshot(), row, localInventory))
            return false;

        if (!IsNetworkSession)
            return TryPurchaseOffline(upgradeId, localInventory);

        if (IsServer)
            return TryPurchaseServer(upgradeId, NetworkManager.Singleton.LocalClientId);

        RequestPurchaseUpgradeRpc(upgradeId);
        return true;
    }

    public bool TryActivate(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key == "0") return false;

        if (activatableMap.TryGetValue(key, out var list) && list != null && list.Count > 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    list[i].SetActive(true);
            }
            return true;
        }

        Debug.LogWarning($"[GreenhouseContext] Activatable key not found: {key} (greenhouseId={ResolveRuntimeId()})");
        return false;
    }

    public void ApplyUpgradeStateLocally(bool playFeedback = false)
    {
        EnsureUpgradeDbLoaded();
        ResetUpgradeRuntimeObjectsToDefaults();
        GreenhouseUpgradeService.ApplyAllSaved(this, runtimeState, upgradeDB, playFeedback);
        OnUpgradeStateChanged?.Invoke();
    }

    public void Capture(SaveGameDTO save)
    {
        if (save?.world == null)
            return;

        if (IsNetworkSession && !IsServer)
            return;

        string currentId = ResolveRuntimeId();
        if (string.IsNullOrWhiteSpace(currentId))
            return;

        runtimeState.greenhouseId = currentId;
        save.world.greenhouseUpgrades.RemoveAll(entry => entry.greenhouseId == currentId);

        var dto = new GreenhouseUpgradeSaveDTO { greenhouseId = currentId };
        for (int i = 0; i < runtimeState.progress.Count; i++)
        {
            dto.progress.Add(new GreenhouseUpgradeProgressDTO
            {
                sort = runtimeState.progress[i].sort,
                completedGrade = runtimeState.progress[i].completedGrade
            });
        }

        save.world.greenhouseUpgrades.Add(dto);
    }

    public void Restore(SaveGameDTO save)
    {
        if (save?.world == null)
            return;

        if (IsNetworkSession && !IsServer)
            return;

        string currentId = ResolveRuntimeId();
        if (string.IsNullOrWhiteSpace(currentId))
            return;

        var dto = save.world.greenhouseUpgrades.Find(entry => entry != null && entry.greenhouseId == currentId);
        if (dto == null)
            return;

        runtimeState.greenhouseId = currentId;
        runtimeState.progress.Clear();

        for (int i = 0; i < dto.progress.Count; i++)
        {
            runtimeState.progress.Add(new GreenhouseUpgradeState.SortProgress
            {
                sort = dto.progress[i].sort,
                completedGrade = dto.progress[i].completedGrade
            });
        }

        ApplyUpgradeStateLocally(false);

        if (IsNetworkSession && IsServer && IsSpawned)
            SyncRuntimeStateToNetwork();
    }

    private bool TryPurchaseOffline(int upgradeId, InventoryItemData inventory)
    {
        EnsureUpgradeDbLoaded();
        if (upgradeDB == null || inventory == null || !upgradeDB.TryGet(upgradeId, out var row))
            return false;

        runtimeState.greenhouseId = ResolveRuntimeId();

        bool purchased = GreenhouseUpgradeService.Purchase(this, runtimeState, row, inventory, true);
        if (!purchased)
            return false;

        AutoSaveService.I?.RequestSave("GreenhouseUpgradeChanged");
        OnUpgradeStateChanged?.Invoke();
        return true;
    }

    private bool TryPurchaseServer(int upgradeId, ulong clientId)
    {
        EnsureUpgradeDbLoaded();
        if (upgradeDB == null || !upgradeDB.TryGet(upgradeId, out var row))
            return false;

        if (!TryGetPlayerInventory(clientId, out var inventory))
            return false;

        var inventoryData = inventory.Data;
        if (inventoryData == null)
            return false;

        runtimeState.greenhouseId = ResolveRuntimeId();

        bool purchased = GreenhouseUpgradeService.Purchase(this, runtimeState, row, inventoryData, false);
        if (!purchased)
            return false;

        SyncRuntimeStateToNetwork();
        AutoSaveService.I?.RequestSave("GreenhouseUpgradeChanged");
        OnUpgradeStateChanged?.Invoke();
        return true;
    }

    private void HandleProgressChanged(NetworkListEvent<GreenhouseUpgradeProgressState> _)
    {
        if (IsServer)
            return;

        ApplySyncedStateToRuntimeState();
    }

    private void ApplySyncedStateToRuntimeState()
    {
        runtimeState.greenhouseId = ResolveRuntimeId();
        runtimeState.progress.Clear();

        for (int i = 0; i < syncedProgress.Count; i++)
        {
            runtimeState.progress.Add(new GreenhouseUpgradeState.SortProgress
            {
                sort = syncedProgress[i].sort,
                completedGrade = syncedProgress[i].completedGrade
            });
        }

        ApplyUpgradeStateLocally(false);
    }

    private void SyncRuntimeStateToNetwork()
    {
        if (!IsServer)
            return;

        runtimeState.greenhouseId = ResolveRuntimeId();

        syncedProgress.Clear();
        for (int i = 0; i < runtimeState.progress.Count; i++)
        {
            syncedProgress.Add(new GreenhouseUpgradeProgressState
            {
                sort = runtimeState.progress[i].sort,
                completedGrade = runtimeState.progress[i].completedGrade
            });
        }
    }

    private void ResetUpgradeRuntimeObjectsToDefaults()
    {
        foreach (var pair in defaultActiveStates)
        {
            if (pair.Key != null)
                pair.Key.SetActive(pair.Value);
        }

        if (sprinklerSystem != null)
            sprinklerSystem.gameObject.SetActive(defaultSprinklerActive);

        if (droneSystem != null)
        {
            droneSystem.gameObject.SetActive(defaultDroneActive);
            droneSystem.SetAutoFertilize(defaultDroneAutoFertilize);
        }
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(greenhouseInstanceId))
            greenhouseInstanceId = Guid.NewGuid().ToString("N");
    }

    private string ResolveRuntimeId()
    {
        var saveable = GetComponent<SaveableEntity>() ?? GetComponentInParent<SaveableEntity>();
        if (saveable != null && !string.IsNullOrWhiteSpace(saveable.PersistentId))
            return $"greenhouse:{saveable.PersistentId}";

        EnsureId();
        return greenhouseInstanceId;
    }

    private void EnsureUpgradeDbLoaded()
    {
        if (upgradeDB == null && FarmPrefabProvider.I != null)
            upgradeDB = FarmPrefabProvider.I.GreenhouseUpgradeDB;
    }

    private void RebuildMap()
    {
        activatableMap.Clear();
        defaultActiveStates.Clear();

        foreach (var entry in activatables)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.target == null)
                continue;

            if (!activatableMap.TryGetValue(entry.key, out var list))
            {
                list = new List<GameObject>();
                activatableMap.Add(entry.key, list);
            }

            list.Add(entry.target);

            if (!defaultActiveStates.ContainsKey(entry.target))
                defaultActiveStates.Add(entry.target, entry.target.activeSelf);
        }
    }

    private bool TryGetPlayerInventory(ulong clientId, out PlayerInventoryRuntime inventory)
    {
        inventory = null;

        if (NetworkManager.Singleton == null)
            return false;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            return false;

        inventory = client.PlayerObject.GetComponent<PlayerInventoryRuntime>();
        return inventory != null;
    }

    private InventoryItemData FindLocalInventory()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            var localPlayer = nm.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
                return localPlayer.GetComponent<PlayerInventoryRuntime>()?.Data;
        }

        return FindFirstObjectByType<PlayerController>()?.Inventory?.Data;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPurchaseUpgradeRpc(int upgradeId, RpcParams rpcParams = default)
    {
        TryPurchaseServer(upgradeId, rpcParams.Receive.SenderClientId);
    }
}





