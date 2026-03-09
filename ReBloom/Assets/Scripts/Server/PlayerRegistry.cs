using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerRegistry : NetworkBehaviour, ISaveable
{
    public static PlayerRegistry I { get; private set; }

    public NetworkVariable<FixedString32Bytes> JoinCode =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<float> ResearchProgressState =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<float> ResearchEnergyState =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<float> ResearchGreeningState =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<PlayerEntry> Players;

    private readonly Dictionary<string, MultiplayerPlayerSaveDTO> pendingPlayerSaves = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MultiplayerPlayerSaveDTO> retainedPlayerSaves = new(StringComparer.OrdinalIgnoreCase);

    public string EntityGuid => "multiplayer_players";

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        Players = new NetworkList<PlayerEntry>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        var code = JoinCodeStore.Current;
        if (!string.IsNullOrEmpty(code))
            JoinCode.Value = new FixedString32Bytes(code);

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        CacheDisconnectedPlayerState(clientId);
        Remove(clientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitNameRpc(ulong clientId, string name)
    {
        if (!IsServer)
            return;

        var fixedName = Sanitize(name);
        Upsert(clientId, fixedName);
    }

    public void ApplyResearchState(float progress, float energy, float greening)
    {
        if (!IsServer)
            return;

        ResearchProgressState.Value = Mathf.Max(0f, progress);
        ResearchEnergyState.Value = Mathf.Max(0f, energy);
        ResearchGreeningState.Value = Mathf.Clamp(greening, 0f, 100f);
    }

    public void Capture(SaveGameDTO save)
    {
        if (save == null)
            return;

        save.multiplayerPlayers ??= new List<MultiplayerPlayerSaveDTO>();
        save.multiplayerPlayers.Clear();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return;

        var snapshot = new Dictionary<string, MultiplayerPlayerSaveDTO>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in retainedPlayerSaves)
            snapshot[pair.Key] = ClonePlayerSave(pair.Value);

        foreach (var pair in pendingPlayerSaves)
            snapshot[pair.Key] = ClonePlayerSave(pair.Value);

        var gates = FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
        foreach (var gate in gates)
        {
            var dto = CapturePlayerState(gate);
            if (dto == null)
                continue;

            snapshot[dto.persistentPlayerId] = ClonePlayerSave(dto);
            retainedPlayerSaves[dto.persistentPlayerId] = ClonePlayerSave(dto);
        }

        foreach (var playerSave in snapshot.Values)
            save.multiplayerPlayers.Add(ClonePlayerSave(playerSave));
    }

    public void Restore(SaveGameDTO save)
    {
        pendingPlayerSaves.Clear();
        retainedPlayerSaves.Clear();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return;

        if (save?.multiplayerPlayers == null)
            return;

        foreach (var playerSave in save.multiplayerPlayers)
        {
            string persistentId = NormalizePersistentId(playerSave?.persistentPlayerId);
            if (string.IsNullOrWhiteSpace(persistentId))
                continue;

            var clone = ClonePlayerSave(playerSave);
            pendingPlayerSaves[persistentId] = clone;
            retainedPlayerSaves[persistentId] = ClonePlayerSave(clone);
        }

        var gates = FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
        foreach (var gate in gates)
            TryApplySavedStateToPlayer(gate);
    }

    public bool TryApplySavedStateToPlayer(NetworkPlayerOwnerGate gate)
    {
        if (gate == null)
            return false;

        string persistentId = NormalizePersistentId(gate.PersistentPlayerIdString);
        if (string.IsNullOrWhiteSpace(persistentId))
            return false;

        if (!pendingPlayerSaves.TryGetValue(persistentId, out var playerSave) &&
            !retainedPlayerSaves.TryGetValue(persistentId, out playerSave))
        {
            return false;
        }

        var clone = ClonePlayerSave(playerSave);
        ApplyPlayerState(gate, clone);
        pendingPlayerSaves.Remove(persistentId);
        retainedPlayerSaves[persistentId] = ClonePlayerSave(clone);
        return true;
    }

    private void CacheDisconnectedPlayerState(ulong clientId)
    {
        var gates = FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
        foreach (var gate in gates)
        {
            if (gate == null || gate.OwnerClientId != clientId)
                continue;

            var dto = CapturePlayerState(gate);
            if (dto == null)
                return;

            retainedPlayerSaves[dto.persistentPlayerId] = ClonePlayerSave(dto);
            pendingPlayerSaves[dto.persistentPlayerId] = ClonePlayerSave(dto);
            return;
        }
    }

    private MultiplayerPlayerSaveDTO CapturePlayerState(NetworkPlayerOwnerGate gate)
    {
        if (gate == null)
            return null;

        string persistentId = NormalizePersistentId(gate.PersistentPlayerIdString);
        if (string.IsNullOrWhiteSpace(persistentId))
            return null;

        var playerStats = gate.GetComponent<PlayerStats>();
        var inventoryRuntime = gate.GetComponent<PlayerInventoryRuntime>();
        var equipData = gate.GetComponent<PlayerEquipData>();
        var playerController = gate.GetComponent<PlayerController>();

        var dto = new MultiplayerPlayerSaveDTO
        {
            persistentPlayerId = persistentId,
            displayName = GetName(gate.OwnerClientId),
            transform = TransformDTO.From(gate.transform),
            hp = playerStats != null ? playerStats.Health.Value : 0f,
            hunger = playerStats != null ? playerStats.Hunger.Value : 0f,
            thirst = playerStats != null ? playerStats.Thirst.Value : 0f,
            pollution = playerStats != null ? playerStats.Pollution.Value : 0f,
            temperature = playerStats != null ? playerStats.Temperature.Value : 0f,
            isDead = gate.IsDeadAuthoritative || (playerController != null && playerController.isDead),
            inventoryTier = inventoryRuntime?.Data != null ? inventoryRuntime.Data.InventoryTier : 0,
            inventoryCapacity = inventoryRuntime?.Data != null ? inventoryRuntime.Data.GetSlotCountForSave() : 0,
            equipment = new EquipmentSaveDTO
            {
                clothItemId = equipData != null && equipData.currentClothEquip != null ? equipData.currentClothEquip.itemID : 0,
                shoesItemId = equipData != null && equipData.currentShoesEquip != null ? equipData.currentShoesEquip.itemID : 0,
                toolItemId = equipData != null && equipData.currentToolEquip != null ? equipData.currentToolEquip.itemID : 0,
            }
        };

        if (inventoryRuntime?.Data != null)
        {
            var slots = inventoryRuntime.Data.GetAllSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.itemID <= 0 || slot.count <= 0)
                    continue;

                dto.inventoryItems.Add(new ItemSlotDTO
                {
                    slot = i,
                    itemId = slot.itemID,
                    amount = slot.count,
                });
            }
        }

        return dto;
    }

    private void ApplyPlayerState(NetworkPlayerOwnerGate gate, MultiplayerPlayerSaveDTO playerSave)
    {
        if (gate == null || playerSave == null)
            return;

        var playerStats = gate.GetComponent<PlayerStats>();
        var playerController = gate.GetComponent<PlayerController>();
        var inventoryRuntime = gate.GetComponent<PlayerInventoryRuntime>();
        var equipManager = gate.GetComponent<PlayerEquipManager>();

        Vector3 position = new(playerSave.transform.px, playerSave.transform.py, playerSave.transform.pz);
        Vector3 rotationEuler = new(playerSave.transform.rx, playerSave.transform.ry, playerSave.transform.rz);
        gate.transform.SetPositionAndRotation(position, Quaternion.Euler(rotationEuler));

        if (playerStats != null)
            playerStats.ApplyRestoredState(playerSave.hp, playerSave.hunger, playerSave.thirst, playerSave.pollution, playerSave.temperature, playerSave.isDead);

        gate.ServerHealth.Value = Mathf.Clamp(playerSave.hp, 0f, playerStats != null ? playerStats.Health.MaxValue : Mathf.Max(playerSave.hp, 0f));
        gate.IsDeadState.Value = playerSave.isDead || playerSave.hp <= 0f;

        if (playerController != null)
        {
            playerController.isDead = playerSave.isDead;
            playerController.SetBlocked(playerSave.isDead);
        }

        ApplyInventory(inventoryRuntime, playerSave);
        ApplyEquipment(equipManager, playerSave.equipment);
        gate.ApplyRestoredOwnerState(position, rotationEuler, playerSave.hp, playerSave.hunger, playerSave.thirst, playerSave.pollution, playerSave.temperature, playerSave.isDead, playerSave.equipment);
    }

    private void ApplyInventory(PlayerInventoryRuntime inventoryRuntime, MultiplayerPlayerSaveDTO playerSave)
    {
        if (inventoryRuntime?.Data == null || playerSave == null)
            return;

        int snapshotLength = Mathf.Max(playerSave.inventoryCapacity, inventoryRuntime.Data.GetSlotCountForSave());
        var snapshot = new InventorySlotSyncState[Mathf.Max(snapshotLength, 0)];

        if (playerSave.inventoryItems != null)
        {
            foreach (var item in playerSave.inventoryItems)
            {
                if (item == null || item.slot < 0 || item.slot >= snapshot.Length)
                    continue;

                snapshot[item.slot] = new InventorySlotSyncState
                {
                    itemID = item.itemId,
                    count = item.amount
                };
            }
        }

        inventoryRuntime.Data.ApplySnapshot(playerSave.inventoryTier, snapshot);
    }

    private void ApplyEquipment(PlayerEquipManager equipManager, EquipmentSaveDTO equipment)
    {
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

    private MultiplayerPlayerSaveDTO ClonePlayerSave(MultiplayerPlayerSaveDTO source)
    {
        if (source == null)
            return null;

        var clone = new MultiplayerPlayerSaveDTO
        {
            persistentPlayerId = source.persistentPlayerId,
            displayName = source.displayName,
            transform = CloneTransform(source.transform),
            hp = source.hp,
            hunger = source.hunger,
            thirst = source.thirst,
            pollution = source.pollution,
            temperature = source.temperature,
            isDead = source.isDead,
            equipment = CloneEquipment(source.equipment),
            inventoryTier = source.inventoryTier,
            inventoryCapacity = source.inventoryCapacity,
            inventoryItems = new List<ItemSlotDTO>()
        };

        if (source.inventoryItems != null)
        {
            foreach (var item in source.inventoryItems)
            {
                if (item == null)
                    continue;

                clone.inventoryItems.Add(new ItemSlotDTO
                {
                    slot = item.slot,
                    itemId = item.itemId,
                    amount = item.amount,
                    durability = item.durability,
                    extraJson = item.extraJson
                });
            }
        }

        return clone;
    }

    private EquipmentSaveDTO CloneEquipment(EquipmentSaveDTO source)
    {
        if (source == null)
            return new EquipmentSaveDTO();

        return new EquipmentSaveDTO
        {
            clothItemId = source.clothItemId,
            shoesItemId = source.shoesItemId,
            toolItemId = source.toolItemId
        };
    }

    private TransformDTO CloneTransform(TransformDTO source)
    {
        if (source == null)
            return new TransformDTO();

        return new TransformDTO
        {
            px = source.px,
            py = source.py,
            pz = source.pz,
            rx = source.rx,
            ry = source.ry,
            rz = source.rz,
            sx = source.sx,
            sy = source.sy,
            sz = source.sz
        };
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
        if (string.IsNullOrWhiteSpace(s))
            s = "Player";

        s = s.Trim();
        if (s.Length > 16)
            s = s.Substring(0, 16);

        return new FixedString32Bytes(s);
    }

    public string GetName(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
                return Players[i].Name.ToString();
        }

        return $"Player#{clientId}";
    }

    private string NormalizePersistentId(string persistentId)
    {
        return string.IsNullOrWhiteSpace(persistentId) ? string.Empty : persistentId.Trim();
    }
}

