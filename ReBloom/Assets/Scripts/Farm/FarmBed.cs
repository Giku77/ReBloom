using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class FarmBed : NetworkBehaviour, ISaveable
{
    [SerializeField] private Transform[] slotPoints;
    [SerializeField] private FarmSlotHighlight[] slotHighlights;
    [SerializeField] private CropSlot[] slots = new CropSlot[8];

    private NetworkList<FarmSlotNetworkState> syncedSlots = new();
    private CancellationTokenSource[] slotCancellationTokens;
    private FarmDB farmDB;
    private GreenhouseContext cachedGreenhouseContext;

    public CropSlot[] Slots => slots;
    public int SlotCount => slots != null ? slots.Length : 0;
    public FarmDB FarmDB => farmDB;
    public string EntityGuid => ResolveEntityGuid();

    public event Action OnChanged;

    private bool IsNetworkSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    private bool HasServerAuthority => !IsNetworkSession || IsServer;

    private void Awake()
    {
        farmDB = FarmPrefabProvider.I != null ? FarmPrefabProvider.I.FarmDB : new FarmDB();
        EnsureSlotsInitialized();
        slotCancellationTokens = new CancellationTokenSource[SlotCount];
        cachedGreenhouseContext = GetComponentInParent<GreenhouseContext>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        syncedSlots.OnListChanged += HandleSyncedSlotsChanged;

        if (IsServer)
            SyncAllSlotsToNetwork();
        else
            ApplyAllSyncedSlotsToLocal(forceVisualRefresh: true);
    }

    public override void OnNetworkDespawn()
    {
        syncedSlots.OnListChanged -= HandleSyncedSlotsChanged;
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (slotCancellationTokens == null)
            return;

        for (int i = 0; i < slotCancellationTokens.Length; i++)
        {
            slotCancellationTokens[i]?.Cancel();
            slotCancellationTokens[i]?.Dispose();
        }
    }

    private void Update()
    {
        if (!HasServerAuthority)
            return;

        float dt = Time.deltaTime;
        for (int i = 0; i < SlotCount; i++)
            TickSlot(i, dt);
    }

    public CropSlot GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return null;

        return slots[index];
    }

    public void SetSlotHighlighted(int index, bool on)
    {
        if (slotHighlights != null && index >= 0 && index < slotHighlights.Length)
            slotHighlights[index]?.SetHighlighted(on);

        var slot = GetSlot(index);
        if (slot?.visual != null)
            slot.visual.SetHighlighted(on);
    }

    public bool CanPlant(int index, int cropId)
    {
        if (index < 0 || index >= SlotCount)
            return false;

        return slots[index].state == CropSlotState.Empty && cropId != 0;
    }

    public bool CanWater(int index)
    {
        var slot = GetSlot(index);
        if (slot == null || slot.state != CropSlotState.Growing)
            return false;

        if (!farmDB.TryGet(slot.cropId, out var row) || row.stages == null || row.stages.Length == 0)
            return false;

        int stageIndex = Mathf.Clamp(slot.stageIndex, 0, row.stages.Length - 1);
        var stage = row.stages[stageIndex];
        return stage.needWater > 0 && slot.wateredCount < stage.needWater;
    }

    public bool CanHarvest(int index)
    {
        var slot = GetSlot(index);
        return slot != null && slot.state == CropSlotState.Mature;
    }

    public bool TryHarvest(int index, out FarmCropRowData row)
    {
        row = null;
        var slot = GetSlot(index);
        if (slot == null || slot.state != CropSlotState.Mature)
            return false;

        return farmDB.TryGet(slot.cropId, out row);
    }

    public bool TryHarvestInternal(int index, out FarmCropRowData row)
    {
        row = null;
        if (!TryHarvest(index, out row))
            return false;

        ClearSlot(index);
        CommitSlotState(index, refreshVisual: true, requestAutosave: true, autosaveReason: "FarmHarvestChanged");
        return true;
    }

    public bool TryApplyFertilizer(int slotIndex, float duration = FarmConst.FertilizerDuration)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.state != CropSlotState.Growing)
            return false;

        slot.growSpeedMultiplier = FarmConst.FertilizerSpeedMul;
        slot.fertilizerRemain = Mathf.Max(slot.fertilizerRemain, duration);
        CommitSlotState(slotIndex, refreshVisual: false, requestAutosave: true, autosaveReason: "FarmFertilizerChanged");
        return true;
    }

    public bool RequestPlantFromLocalPlayer(int index, int seedItemId)
    {
        if (!farmDB.TryGetBySeedId(seedItemId, out var cropRow) || !CanPlant(index, cropRow.cropId))
            return false;

        if (!IsNetworkSession)
        {
            var inventory = FindLocalInventoryData();
            if (inventory == null || !inventory.TryRemoveItem(seedItemId, 1))
                return false;

            PlantLocal(index, cropRow.cropId);
            return true;
        }

        if (IsServer)
            return TryPlantServer(index, seedItemId, NetworkManager.Singleton.LocalClientId);

        RequestPlantRpc(index, seedItemId);
        return true;
    }

    public bool RequestWaterFromLocalPlayer(int index)
    {
        if (!CanWater(index))
            return false;

        if (!IsNetworkSession)
        {
            var inventory = FindLocalInventoryData();
            return inventory != null && TryWaterLocal(index, inventory, FarmConst.WaterItemId);
        }

        if (IsServer)
            return TryWaterServer(index, NetworkManager.Singleton.LocalClientId);

        RequestWaterRpc(index);
        return true;
    }

    public bool RequestFertilizeFromLocalPlayer(int index)
    {
        var slot = GetSlot(index);
        if (slot == null || slot.state != CropSlotState.Growing)
            return false;

        if (!IsNetworkSession)
        {
            var inventory = FindLocalInventoryData();
            return inventory != null && TryFertilizeLocal(index, inventory, FarmConst.FertilizerItemId, FarmConst.FertilizerDuration);
        }

        if (IsServer)
            return TryFertilizeServer(index, NetworkManager.Singleton.LocalClientId);

        RequestFertilizeRpc(index);
        return true;
    }

    public bool RequestHarvestFromLocalPlayer(int index)
    {
        if (!CanHarvest(index))
            return false;

        if (!IsNetworkSession)
        {
            var player = FindLocalPlayer();
            if (player == null)
                return false;

            Harvest(index, player);
            return true;
        }

        if (IsServer)
            return TryHarvestServer(index, NetworkManager.Singleton.LocalClientId);

        RequestHarvestRpc(index);
        return true;
    }

    public bool RequestUprootFromLocalPlayer(int index)
    {
        var slot = GetSlot(index);
        if (slot == null || slot.state == CropSlotState.Empty)
            return false;

        if (!IsNetworkSession)
        {
            Uproot(index);
            return true;
        }

        if (IsServer)
            return TryUprootServer(index);

        RequestUprootRpc(index);
        return true;
    }

    public void Plant(int index, int cropId)
    {
        PlantLocal(index, cropId);
    }

    public void Water(int index)
    {
        if (!CanWater(index))
            return;

        slots[index].wateredCount++;
        CommitSlotState(index, refreshVisual: false, requestAutosave: true, autosaveReason: "FarmWaterChanged");
    }

    public void Harvest(int index, PlayerController player)
    {
        if (player == null || !TryHarvest(index, out var row))
            return;

        foreach (var drop in row.drops)
        {
            if (drop.rate < 1f && UnityEngine.Random.value > drop.rate)
                continue;

            player.Inventory.AddItemFromWorld(drop.itemId, drop.count);
        }

        ClearSlot(index);
        CommitSlotState(index, refreshVisual: true, requestAutosave: true, autosaveReason: "FarmHarvestChanged");
    }

    public void Uproot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return;

        if (slots[index].state == CropSlotState.Empty)
            return;

        ClearSlot(index);
        CommitSlotState(index, refreshVisual: true, requestAutosave: true, autosaveReason: "FarmUprootChanged");
    }

    public bool TryWaterByPlayer(int index, InventoryItemData inv, int waterItemId, int consume = 1)
    {
        if (!CanWater(index) || inv == null)
            return false;

        if (inv.GetItemCount(waterItemId) < consume)
            return false;

        if (!inv.TryRemoveItem(waterItemId, consume))
            return false;

        Water(index);
        return true;
    }

    public bool TryFertilizeByPlayer(int index, InventoryItemData inv, int fertilizerItemId, float duration)
    {
        var slot = GetSlot(index);
        if (slot == null || slot.state != CropSlotState.Growing || inv == null)
            return false;

        if (inv.GetItemCount(fertilizerItemId) <= 0)
            return false;

        if (!inv.TryRemoveItem(fertilizerItemId, 1))
            return false;

        if (!TryApplyFertilizer(index, duration))
        {
            inv.TryAddItem(fertilizerItemId, 1);
            return false;
        }

        return true;
    }

    public void Capture(SaveGameDTO save)
    {
        if (save?.world == null)
            return;

        if (IsNetworkSession && !IsServer)
            return;

        string entityGuid = ResolveEntityGuid();
        save.world.farmBeds.RemoveAll(entry => entry.guid == entityGuid);

        var dto = new FarmBedSaveDTO { guid = entityGuid };
        for (int i = 0; i < SlotCount; i++)
        {
            var slot = slots[i];
            dto.slots.Add(new FarmSlotSaveDTO
            {
                state = (int)slot.state,
                cropId = slot.cropId,
                stageIndex = slot.stageIndex,
                stageTimer = slot.stageTimer,
                wateredCount = slot.wateredCount,
                fertilizerRemain = slot.fertilizerRemain,
                growSpeedMultiplier = slot.growSpeedMultiplier
            });
        }

        save.world.farmBeds.Add(dto);
        Debug.Log($"[FarmBed Capture] guid={entityGuid} slots={dto.slots.Count} scene={gameObject.scene.name}");
    }

    public void Restore(SaveGameDTO save)
    {
        if (save?.world == null)
            return;

        if (IsNetworkSession && !IsServer)
            return;

        string entityGuid = ResolveEntityGuid();
        var dto = save.world.farmBeds.Find(entry => entry != null && entry.guid == entityGuid);
        if (dto == null)
        {
            Debug.LogWarning($"[FarmBed Restore] Save entry not found. guid={entityGuid} available={save.world.farmBeds.Count}");
            return;
        }

        EnsureSlotsInitialized();

        int count = Mathf.Min(dto.slots.Count, SlotCount);
        for (int i = 0; i < count; i++)
            ApplySaveSlotToLocal(i, dto.slots[i], refreshVisual: true);

        for (int i = count; i < SlotCount; i++)
            ApplySaveSlotToLocal(i, new FarmSlotSaveDTO(), refreshVisual: true);

        if (IsNetworkSession && IsServer && IsSpawned)
            SyncAllSlotsToNetwork();

        Debug.Log($"[FarmBed Restore] guid={entityGuid} restoredSlots={dto.slots.Count}");
        OnChanged?.Invoke();
    }

    private void TickSlot(int index, float dt)
    {
        var slot = slots[index];
        if (slot.state != CropSlotState.Growing)
            return;

        if (!farmDB.TryGet(slot.cropId, out var row) || row.stages == null || row.stages.Length == 0)
            return;

        int stageIndex = Mathf.Clamp(slot.stageIndex, 0, row.stages.Length - 1);
        var stage = row.stages[stageIndex];
        if (stage.needWater > 0 && slot.wateredCount < stage.needWater)
            return;

        float previousStageTimer = slot.stageTimer;
        float previousFertilizerRemain = slot.fertilizerRemain;
        int previousStageIndex = slot.stageIndex;
        CropSlotState previousState = slot.state;

        if (slot.fertilizerRemain > 0f)
        {
            slot.fertilizerRemain -= dt;
            if (slot.fertilizerRemain <= 0f)
            {
                slot.fertilizerRemain = 0f;
                slot.growSpeedMultiplier = 1f;
            }
        }

        float growDt = dt * Mathf.Max(1f, slot.growSpeedMultiplier <= 0f ? 1f : slot.growSpeedMultiplier);
        slot.stageTimer += growDt;

        bool majorStateChanged = false;
        if (slot.stageTimer >= stage.needTime)
        {
            slot.stageTimer = 0f;
            slot.wateredCount = 0;
            slot.stageIndex++;

            if (slot.stageIndex >= row.stages.Length - 1)
                slot.state = CropSlotState.Mature;

            majorStateChanged = true;
        }

        bool timerBucketChanged = Mathf.FloorToInt(previousStageTimer) != Mathf.FloorToInt(slot.stageTimer);
        bool fertilizerBucketChanged = Mathf.FloorToInt(previousFertilizerRemain) != Mathf.FloorToInt(slot.fertilizerRemain);
        bool saveBucketChanged = Mathf.FloorToInt(previousStageTimer / 5f) != Mathf.FloorToInt(slot.stageTimer / 5f)
            || Mathf.FloorToInt(previousFertilizerRemain / 5f) != Mathf.FloorToInt(slot.fertilizerRemain / 5f);

        if (!majorStateChanged && !timerBucketChanged && !fertilizerBucketChanged)
            return;

        bool refreshVisual = majorStateChanged || previousStageIndex != slot.stageIndex || previousState != slot.state;
        CommitSlotState(index, refreshVisual, requestAutosave: majorStateChanged || saveBucketChanged, autosaveReason: "FarmProgressChanged");
    }

    private void PlantLocal(int index, int cropId)
    {
        var slot = slots[index];
        slot.state = CropSlotState.Growing;
        slot.cropId = cropId;
        slot.stageIndex = 0;
        slot.stageTimer = 0f;
        slot.wateredCount = 0;
        slot.fertilizerRemain = 0f;
        slot.growSpeedMultiplier = 1f;

        CommitSlotState(index, refreshVisual: true, requestAutosave: true, autosaveReason: "FarmPlantChanged");
    }

    private bool TryWaterLocal(int index, InventoryItemData inventory, int waterItemId)
    {
        if (!CanWater(index) || inventory == null)
            return false;

        if (!inventory.TryRemoveItem(waterItemId, 1))
            return false;

        Water(index);
        return true;
    }

    private bool TryFertilizeLocal(int index, InventoryItemData inventory, int fertilizerItemId, float duration)
    {
        if (inventory == null)
            return false;

        if (!inventory.TryRemoveItem(fertilizerItemId, 1))
            return false;

        if (!TryApplyFertilizer(index, duration))
        {
            inventory.TryAddItem(fertilizerItemId, 1);
            return false;
        }

        return true;
    }

    private bool TryPlantServer(int index, int seedItemId, ulong clientId)
    {
        if (!TryResolvePlayerInventory(clientId, out var inventory))
            return false;

        if (!farmDB.TryGetBySeedId(seedItemId, out var cropRow))
            return false;

        if (!CanPlant(index, cropRow.cropId))
            return false;

        if (!inventory.TryRemoveItem(seedItemId, 1))
            return false;

        PlantLocal(index, cropRow.cropId);
        return true;
    }

    private bool TryWaterServer(int index, ulong clientId)
    {
        if (!TryResolvePlayerInventory(clientId, out var inventory))
            return false;

        if (!CanWater(index))
            return false;

        if (!inventory.TryRemoveItem(FarmConst.WaterItemId, 1))
            return false;

        Water(index);
        return true;
    }

    private bool TryFertilizeServer(int index, ulong clientId)
    {
        if (!TryResolvePlayerInventory(clientId, out var inventory))
            return false;

        var slot = GetSlot(index);
        if (slot == null || slot.state != CropSlotState.Growing)
            return false;

        if (!inventory.TryRemoveItem(FarmConst.FertilizerItemId, 1))
            return false;

        if (!TryApplyFertilizer(index, FarmConst.FertilizerDuration))
        {
            inventory.TryAddItem(FarmConst.FertilizerItemId, 1);
            return false;
        }

        return true;
    }

    private bool TryHarvestServer(int index, ulong clientId)
    {
        if (!TryResolvePlayerInventory(clientId, out var inventory))
            return false;

        if (!TryHarvest(index, out var row))
            return false;

        foreach (var drop in row.drops)
        {
            if (drop.rate < 1f && UnityEngine.Random.value > drop.rate)
                continue;

            inventory.AddItemFromWorld(drop.itemId, drop.count);
        }

        ClearSlot(index);
        CommitSlotState(index, refreshVisual: true, requestAutosave: true, autosaveReason: "FarmHarvestChanged");
        return true;
    }

    private bool TryUprootServer(int index)
    {
        var slot = GetSlot(index);
        if (slot == null || slot.state == CropSlotState.Empty)
            return false;

        Uproot(index);
        return true;
    }

    private void HandleSyncedSlotsChanged(NetworkListEvent<FarmSlotNetworkState> _)
    {
        if (IsServer)
            return;

        ApplyAllSyncedSlotsToLocal(forceVisualRefresh: false);
    }

    private void ApplyAllSyncedSlotsToLocal(bool forceVisualRefresh)
    {
        EnsureSlotsInitialized();

        int count = Mathf.Min(syncedSlots.Count, SlotCount);
        bool changed = false;

        for (int i = 0; i < count; i++)
            changed |= ApplyNetworkSlotToLocal(i, syncedSlots[i], forceVisualRefresh);

        for (int i = count; i < SlotCount; i++)
            changed |= ApplyNetworkSlotToLocal(i, default, forceVisualRefresh);

        if (changed)
            OnChanged?.Invoke();
    }

    private bool ApplyNetworkSlotToLocal(int index, FarmSlotNetworkState state, bool forceVisualRefresh)
    {
        var slot = slots[index];
        bool visualChanged = forceVisualRefresh
            || slot.state != (CropSlotState)state.state
            || slot.cropId != state.cropId
            || slot.stageIndex != state.stageIndex;

        bool changed = visualChanged
            || !Mathf.Approximately(slot.stageTimer, state.stageTimer)
            || slot.wateredCount != state.wateredCount
            || !Mathf.Approximately(slot.fertilizerRemain, state.fertilizerRemain)
            || !Mathf.Approximately(slot.growSpeedMultiplier, state.growSpeedMultiplier);

        slot.state = (CropSlotState)state.state;
        slot.cropId = state.cropId;
        slot.stageIndex = state.stageIndex;
        slot.stageTimer = state.stageTimer;
        slot.wateredCount = state.wateredCount;
        slot.fertilizerRemain = state.fertilizerRemain;
        slot.growSpeedMultiplier = state.growSpeedMultiplier <= 0f ? 1f : state.growSpeedMultiplier;

        if (visualChanged)
            UpdateSlotVisual(index);

        return changed;
    }

    private void SyncAllSlotsToNetwork()
    {
        if (!IsServer)
            return;

        syncedSlots.Clear();
        for (int i = 0; i < SlotCount; i++)
            syncedSlots.Add(BuildNetworkState(slots[i]));
    }

    private void CommitSlotState(int index, bool refreshVisual, bool requestAutosave, string autosaveReason)
    {
        if (refreshVisual)
            UpdateSlotVisual(index);

        if (IsNetworkSession && IsServer && IsSpawned)
        {
            if (index < syncedSlots.Count)
                syncedSlots[index] = BuildNetworkState(slots[index]);
            else
                SyncAllSlotsToNetwork();
        }

        if (requestAutosave)
            AutoSaveService.I?.RequestSave(autosaveReason);

        OnChanged?.Invoke();
    }

    private FarmSlotNetworkState BuildNetworkState(CropSlot slot)
    {
        return new FarmSlotNetworkState
        {
            state = (int)slot.state,
            cropId = slot.cropId,
            stageIndex = slot.stageIndex,
            stageTimer = slot.stageTimer,
            wateredCount = slot.wateredCount,
            fertilizerRemain = slot.fertilizerRemain,
            growSpeedMultiplier = slot.growSpeedMultiplier
        };
    }

    private void ClearSlot(int index)
    {
        var slot = slots[index];
        slot.state = CropSlotState.Empty;
        slot.cropId = 0;
        slot.stageIndex = 0;
        slot.stageTimer = 0f;
        slot.wateredCount = 0;
        slot.fertilizerRemain = 0f;
        slot.growSpeedMultiplier = 1f;
    }

    private void ApplySaveSlotToLocal(int index, FarmSlotSaveDTO save, bool refreshVisual)
    {
        var slot = slots[index];
        slot.state = (CropSlotState)save.state;
        slot.cropId = save.cropId;
        slot.stageIndex = save.stageIndex;
        slot.stageTimer = save.stageTimer;
        slot.wateredCount = save.wateredCount;
        slot.fertilizerRemain = save.fertilizerRemain;
        slot.growSpeedMultiplier = save.growSpeedMultiplier <= 0f ? 1f : save.growSpeedMultiplier;

        if (refreshVisual)
            UpdateSlotVisual(index);
    }

    private void EnsureSlotsInitialized()
    {
        if (slots == null || slots.Length == 0)
            slots = new CropSlot[8];

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new CropSlot();

            if (slots[i].growSpeedMultiplier <= 0f)
                slots[i].growSpeedMultiplier = 1f;
        }
    }

    private async UniTaskVoid UpdateSlotVisualAsync(int index)
    {
        if (slotPoints == null || index < 0 || index >= slotPoints.Length)
            return;

        if (slotCancellationTokens == null || index < 0 || index >= slotCancellationTokens.Length)
            return;

        slotCancellationTokens[index]?.Cancel();
        slotCancellationTokens[index]?.Dispose();
        slotCancellationTokens[index] = new CancellationTokenSource();
        var token = slotCancellationTokens[index].Token;

        var slot = slots[index];
        var point = slotPoints[index];

        if (slot.visualRoot != null)
        {
            FarmPrefabProvider.I?.ReleaseAddressableInstance(slot.visualRoot);
            slot.visualRoot = null;
            slot.visual = null;
        }

        if (slot.state == CropSlotState.Empty)
            return;

        if (!farmDB.TryGet(slot.cropId, out var row) || row.stages == null || row.stages.Length == 0)
            return;

        string prefabKey = null;
        if (slot.state == CropSlotState.Growing)
        {
            int stageIndex = Mathf.Clamp(slot.stageIndex, 0, row.stages.Length - 1);
            prefabKey = row.stages[stageIndex].prefabKey;
        }
        else if (slot.state == CropSlotState.Mature)
        {
            prefabKey = row.stages[row.stages.Length - 1].prefabKey;
        }

        if (string.IsNullOrEmpty(prefabKey) || FarmPrefabProvider.I == null)
            return;

        var go = await FarmPrefabProvider.I.InstantiateAddressableAsync(prefabKey, point.position, point.rotation, point);
        if (token.IsCancellationRequested)
        {
            if (go != null)
                FarmPrefabProvider.I.ReleaseAddressableInstance(go);
            return;
        }

        slot.visualRoot = go;
        slot.visual = go != null ? go.GetComponentInChildren<CropVisual>(true) : null;
    }

    private void UpdateSlotVisual(int index)
    {
        UpdateSlotVisualAsync(index).Forget();
    }

    private bool TryResolvePlayerInventory(ulong clientId, out PlayerInventoryRuntime inventory)
    {
        inventory = null;
        var nm = NetworkManager.Singleton;
        if (nm == null)
            return false;

        if (!nm.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            return false;

        inventory = client.PlayerObject.GetComponent<PlayerInventoryRuntime>();
        return inventory != null;
    }

    private PlayerController FindLocalPlayer()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            var localPlayer = nm.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
                return localPlayer.GetComponent<PlayerController>();
        }

        return FindFirstObjectByType<PlayerController>();
    }

    private InventoryItemData FindLocalInventoryData()
    {
        return FindLocalPlayer()?.Inventory?.Data;
    }

    private string ResolveEntityGuid()
    {
        if (cachedGreenhouseContext == null)
            cachedGreenhouseContext = GetComponentInParent<GreenhouseContext>();

        string ownerId = cachedGreenhouseContext != null && !string.IsNullOrWhiteSpace(cachedGreenhouseContext.Id)
            ? cachedGreenhouseContext.Id
            : null;

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            var saveable = GetComponent<SaveableEntity>() ?? GetComponentInParent<SaveableEntity>();
            if (saveable != null && !string.IsNullOrWhiteSpace(saveable.PersistentId))
                ownerId = $"saveable:{saveable.PersistentId}";
        }

        if (string.IsNullOrWhiteSpace(ownerId))
            ownerId = $"scene:{gameObject.scene.name}";

        return $"farmbed:{ownerId}:{BuildRelativePath()}";
    }

    private string BuildRelativePath()
    {
        if (cachedGreenhouseContext == null)
            cachedGreenhouseContext = GetComponentInParent<GreenhouseContext>();

        Transform root = cachedGreenhouseContext != null ? cachedGreenhouseContext.transform : transform.root;
        var parts = new List<string>();
        Transform current = transform;

        while (current != null && current != root)
        {
            parts.Add($"{current.name}_{current.GetSiblingIndex()}");
            current = current.parent;
        }

        parts.Reverse();
        return parts.Count == 0 ? $"{transform.name}_{transform.GetSiblingIndex()}" : string.Join("/", parts);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlantRpc(int index, int seedItemId, RpcParams rpcParams = default)
    {
        TryPlantServer(index, seedItemId, rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestWaterRpc(int index, RpcParams rpcParams = default)
    {
        TryWaterServer(index, rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestFertilizeRpc(int index, RpcParams rpcParams = default)
    {
        TryFertilizeServer(index, rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestHarvestRpc(int index, RpcParams rpcParams = default)
    {
        TryHarvestServer(index, rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestUprootRpc(int index, RpcParams rpcParams = default)
    {
        TryUprootServer(index);
    }
}