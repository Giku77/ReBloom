using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class NetworkStorageContainer : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("Drop Settings")]
    [SerializeField] private float dropForwardOffset = 1.25f;
    [SerializeField] private float dropUpOffset = 0.75f;

    private readonly NetworkVariable<int> slotCount = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkList<NetworkStorageSlotState> slots;
    private StorageData mirrorData;

    private bool IsNetworkSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        slots = new NetworkList<NetworkStorageSlotState>();

        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
    }

    public void BindMirror(StorageData data)
    {
        mirrorData = data;

        if (mirrorData == null)
            return;

        if (IsServer && slotCount.Value <= 0)
            slotCount.Value = mirrorData.SlotCount;

        if (IsSpawned && !IsServer)
            ApplyStateToMirror();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        slots.OnListChanged += HandleSlotsChanged;
        slotCount.OnValueChanged += HandleSlotCountChanged;

        if (IsServer)
        {
            if (mirrorData != null && slotCount.Value <= 0)
                slotCount.Value = mirrorData.SlotCount;

            SyncFromMirrorToNetwork();
        }
        else
        {
            ApplyStateToMirror();
        }
    }

    public override void OnNetworkDespawn()
    {
        slots.OnListChanged -= HandleSlotsChanged;
        slotCount.OnValueChanged -= HandleSlotCountChanged;
        base.OnNetworkDespawn();
    }

    public bool RequestDepositFromLocalPlayer(int itemID, int count)
    {
        if (count <= 0)
            return false;

        if (!IsNetworkSession || !IsSpawned)
            return DepositFromLocalPlayer(itemID, count);

        if (IsServer)
            return DepositFromPlayerServer(itemID, count, NetworkManager.Singleton.LocalClientId);

        RequestDepositFromPlayerRpc(itemID, count);
        return true;
    }

    public bool RequestWithdrawToLocalPlayer(int itemID, int count)
    {
        if (count <= 0)
            return false;

        if (!IsNetworkSession || !IsSpawned)
            return WithdrawToLocalPlayer(itemID, count);

        if (IsServer)
            return WithdrawToPlayerServer(itemID, count, NetworkManager.Singleton.LocalClientId);

        RequestWithdrawToPlayerRpc(itemID, count);
        return true;
    }

    public bool RequestDepositAllFromLocalPlayer()
    {
        if (!IsNetworkSession || !IsSpawned)
            return DepositAllFromLocalPlayer();

        if (IsServer)
            return DepositAllFromPlayerServer(NetworkManager.Singleton.LocalClientId);

        RequestDepositAllFromPlayerRpc();
        return true;
    }

    public bool RequestWithdrawAllToLocalPlayer()
    {
        if (!IsNetworkSession || !IsSpawned)
            return WithdrawAllToLocalPlayer();

        if (IsServer)
            return WithdrawAllToPlayerServer(NetworkManager.Singleton.LocalClientId);

        RequestWithdrawAllToPlayerRpc();
        return true;
    }

    public bool RequestDropToWorldFromLocalPlayer(int itemID, int count)
    {
        if (count <= 0)
            return false;

        if (!IsNetworkSession || !IsSpawned)
        {
            DropToWorldLocal(itemID, count).Forget();
            return true;
        }

        if (IsServer)
        {
            DropToWorldServer(itemID, count, NetworkManager.Singleton.LocalClientId).Forget();
            return true;
        }

        RequestDropToWorldRpc(itemID, count);
        return true;
    }

    public bool ServerTryAddItem(int itemID, int count)
    {
        if (count <= 0 || mirrorData == null)
            return false;

        if (IsNetworkSession && !IsServer)
            return false;

        int added = mirrorData.AddItem(itemID, count);
        if (added <= 0)
            return false;

        if (IsServer)
            SyncFromMirrorToNetwork();
        else
            mirrorData.NotifyStorageChanged();

        return true;
    }

    public bool ServerTryRemoveItem(int itemID, int count)
    {
        if (count <= 0 || mirrorData == null)
            return false;

        if (IsNetworkSession && !IsServer)
            return false;

        if (!mirrorData.TryRemoveItem(itemID, count))
            return false;

        if (IsServer)
            SyncFromMirrorToNetwork();
        else
            mirrorData.NotifyStorageChanged();

        return true;
    }

    public void ServerClear()
    {
        if (mirrorData == null)
            return;

        if (IsNetworkSession && !IsServer)
            return;

        mirrorData.Clear();

        if (IsServer)
            SyncFromMirrorToNetwork();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDepositFromPlayerRpc(int itemID, int count, RpcParams rpcParams = default)
    {
        DepositFromPlayerServer(itemID, count, rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestWithdrawToPlayerRpc(int itemID, int count, RpcParams rpcParams = default)
    {
        WithdrawToPlayerServer(itemID, count, rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDepositAllFromPlayerRpc(RpcParams rpcParams = default)
    {
        DepositAllFromPlayerServer(rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestWithdrawAllToPlayerRpc(RpcParams rpcParams = default)
    {
        WithdrawAllToPlayerServer(rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDropToWorldRpc(int itemID, int count, RpcParams rpcParams = default)
    {
        DropToWorldServer(itemID, count, rpcParams.Receive.SenderClientId).Forget();
    }

    private void HandleSlotsChanged(NetworkListEvent<NetworkStorageSlotState> _)
    {
        if (IsServer)
            return;

        ApplyStateToMirror();
    }

    private void HandleSlotCountChanged(int _, int __)
    {
        if (IsServer)
            return;

        ApplyStateToMirror();
    }

    private bool DepositFromLocalPlayer(int itemID, int count)
    {
        var inventory = FindFirstObjectByType<PlayerController>()?.Inventory;
        if (inventory == null || mirrorData == null)
            return false;

        int available = inventory.GetItemCount(itemID);
        int requested = Mathf.Min(count, available);
        if (requested <= 0)
            return false;

        int added = mirrorData.AddItem(itemID, requested);
        if (added <= 0)
            return false;

        if (!inventory.TryRemoveItem(itemID, added))
        {
            mirrorData.TryRemoveItem(itemID, added);
            return false;
        }

        mirrorData.NotifyStorageChanged();
        return true;
    }

    private bool WithdrawToLocalPlayer(int itemID, int count)
    {
        var inventory = FindFirstObjectByType<PlayerController>()?.Inventory;
        if (inventory == null || mirrorData == null)
            return false;

        int available = mirrorData.GetItemCount(itemID);
        int requested = Mathf.Min(count, available);
        if (requested <= 0)
            return false;

        int added = inventory.AddItemWithOverflow(itemID, requested, out _);
        if (added <= 0)
            return false;

        if (!mirrorData.TryRemoveItem(itemID, added))
        {
            inventory.TryRemoveItem(itemID, added);
            return false;
        }

        mirrorData.NotifyStorageChanged();
        return true;
    }

    private bool DepositAllFromLocalPlayer()
    {
        var inventory = FindFirstObjectByType<PlayerController>()?.Inventory;
        if (inventory == null || mirrorData == null)
            return false;

        bool movedAny = false;
        foreach (var slot in inventory.GetAllSlots())
        {
            if (slot == null || slot.itemID <= 0 || slot.count <= 0)
                continue;

            int added = mirrorData.AddItem(slot.itemID, slot.count);
            if (added <= 0)
                continue;

            if (!inventory.TryRemoveItem(slot.itemID, added))
            {
                mirrorData.TryRemoveItem(slot.itemID, added);
                continue;
            }

            movedAny = true;
        }

        if (movedAny)
            mirrorData.NotifyStorageChanged();

        return movedAny;
    }

    private bool WithdrawAllToLocalPlayer()
    {
        var inventory = FindFirstObjectByType<PlayerController>()?.Inventory;
        if (inventory == null || mirrorData == null)
            return false;

        bool movedAny = false;
        var snapshot = new List<ItemSlotData>(mirrorData.Items);
        foreach (var slot in snapshot)
        {
            if (slot == null || slot.itemID <= 0 || slot.count <= 0)
                continue;

            int added = inventory.AddItemWithOverflow(slot.itemID, slot.count, out _);
            if (added <= 0)
                continue;

            if (!mirrorData.TryRemoveItem(slot.itemID, added))
            {
                inventory.TryRemoveItem(slot.itemID, added);
                continue;
            }

            movedAny = true;
        }

        if (movedAny)
            mirrorData.NotifyStorageChanged();

        return movedAny;
    }

    private bool DepositFromPlayerServer(int itemID, int count, ulong clientId)
    {
        if (!TryResolvePlayer(clientId, out var inventory, out _))
            return false;

        if (mirrorData == null)
            return false;

        int available = inventory.GetItemCount(itemID);
        int requested = Mathf.Min(count, available);
        if (requested <= 0)
            return false;

        int added = mirrorData.AddItem(itemID, requested);
        if (added <= 0)
            return false;

        if (!inventory.TryRemoveItem(itemID, added))
        {
            mirrorData.TryRemoveItem(itemID, added);
            return false;
        }

        SyncFromMirrorToNetwork();
        return true;
    }

    private bool WithdrawToPlayerServer(int itemID, int count, ulong clientId)
    {
        if (!TryResolvePlayer(clientId, out var inventory, out _))
            return false;

        if (mirrorData == null)
            return false;

        int available = mirrorData.GetItemCount(itemID);
        int requested = Mathf.Min(count, available);
        if (requested <= 0)
            return false;

        int added = inventory.AddItemWithOverflow(itemID, requested, out _);
        if (added <= 0)
            return false;

        if (!mirrorData.TryRemoveItem(itemID, added))
        {
            inventory.TryRemoveItem(itemID, added);
            return false;
        }

        SyncFromMirrorToNetwork();
        return true;
    }

    private bool DepositAllFromPlayerServer(ulong clientId)
    {
        if (!TryResolvePlayer(clientId, out var inventory, out _))
            return false;

        if (mirrorData == null)
            return false;

        bool movedAny = false;
        foreach (var slot in inventory.GetAllSlots())
        {
            if (slot == null || slot.itemID <= 0 || slot.count <= 0)
                continue;

            int added = mirrorData.AddItem(slot.itemID, slot.count);
            if (added <= 0)
                continue;

            if (!inventory.TryRemoveItem(slot.itemID, added))
            {
                mirrorData.TryRemoveItem(slot.itemID, added);
                continue;
            }

            movedAny = true;
        }

        if (movedAny)
            SyncFromMirrorToNetwork();

        return movedAny;
    }

    private bool WithdrawAllToPlayerServer(ulong clientId)
    {
        if (!TryResolvePlayer(clientId, out var inventory, out _))
            return false;

        if (mirrorData == null)
            return false;

        bool movedAny = false;
        var snapshot = new List<ItemSlotData>(mirrorData.Items);
        foreach (var slot in snapshot)
        {
            if (slot == null || slot.itemID <= 0 || slot.count <= 0)
                continue;

            int added = inventory.AddItemWithOverflow(slot.itemID, slot.count, out _);
            if (added <= 0)
                continue;

            if (!mirrorData.TryRemoveItem(slot.itemID, added))
            {
                inventory.TryRemoveItem(slot.itemID, added);
                continue;
            }

            movedAny = true;
        }

        if (movedAny)
            SyncFromMirrorToNetwork();

        return movedAny;
    }

    private async UniTaskVoid DropToWorldLocal(int itemID, int count)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.Inventory == null || mirrorData == null)
            return;

        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null)
            return;

        int available = mirrorData.GetItemCount(itemID);
        int requested = Mathf.Min(count, available);
        if (requested <= 0)
            return;

        if (!mirrorData.TryRemoveItem(itemID, requested))
            return;

        Vector3 dropPos = player.transform.position + player.transform.forward * dropForwardOffset + Vector3.up * dropUpOffset;
        await ResolveItemSpawner().DropItemWithQuantity(item, dropPos, requested);
        mirrorData.NotifyStorageChanged();
    }

    private async UniTaskVoid DropToWorldServer(int itemID, int count, ulong clientId)
    {
        if (!TryResolvePlayer(clientId, out _, out var playerTransform))
            return;

        if (mirrorData == null)
            return;

        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null)
            return;

        int available = mirrorData.GetItemCount(itemID);
        int requested = Mathf.Min(count, available);
        if (requested <= 0)
            return;

        if (!mirrorData.TryRemoveItem(itemID, requested))
            return;

        SyncFromMirrorToNetwork();

        Vector3 dropPos = playerTransform.position + playerTransform.forward * dropForwardOffset + Vector3.up * dropUpOffset;
        await ResolveItemSpawner().DropItemWithQuantity(item, dropPos, requested);
    }

    private void SyncFromMirrorToNetwork()
    {
        if (!IsServer || mirrorData == null)
            return;

        slotCount.Value = mirrorData.SlotCount;

        slots.Clear();
        foreach (var slot in mirrorData.Items)
        {
            if (slot == null || slot.itemID <= 0 || slot.count <= 0)
                continue;

            slots.Add(new NetworkStorageSlotState
            {
                itemID = slot.itemID,
                count = slot.count
            });
        }

        mirrorData.NotifyStorageChanged();
    }

    private void ApplyStateToMirror()
    {
        if (mirrorData == null)
            return;

        mirrorData.Clear();

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.itemID <= 0 || slot.count <= 0)
                continue;

            mirrorData.AddItem(slot.itemID, slot.count);
        }

        mirrorData.NotifyStorageChanged();
    }

    private bool TryResolvePlayer(ulong clientId, out PlayerInventoryRuntime inventory, out Transform playerTransform)
    {
        inventory = null;
        playerTransform = null;

        if (NetworkManager.Singleton == null)
            return false;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return false;

        var playerObj = client.PlayerObject;
        if (playerObj == null)
            return false;

        inventory = playerObj.GetComponent<PlayerInventoryRuntime>();
        playerTransform = playerObj.transform;
        return inventory != null;
    }

    private ItemSpawner ResolveItemSpawner()
    {
        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();

        return itemSpawner;
    }
}


