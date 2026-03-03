using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkWorldItem : NetworkBehaviour
{
    [SerializeField] private WorldItem worldItem;

    private NetworkVariable<int> itemID = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> quantity = new(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> persistent = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool pendingApply;

    private void Awake()
    {
        if (worldItem == null)
            worldItem = GetComponent<WorldItem>();
    }

    public override void OnNetworkSpawn()
    {
        itemID.OnValueChanged += OnItemChanged;
        quantity.OnValueChanged += OnQuantityChanged;
        persistent.OnValueChanged += OnPersistentChanged;

        if (itemID.Value > 0)
            TryApplyOrDefer();
        else
            Debug.LogWarning("[NetworkWorldItem] OnNetworkSpawn 시 itemID가 아직 0임");
    }

    public override void OnNetworkDespawn()
    {
        itemID.OnValueChanged -= OnItemChanged;
        quantity.OnValueChanged -= OnQuantityChanged;
        persistent.OnValueChanged -= OnPersistentChanged;
    }

    public void InitializeServer(ItemBase item, int qty, bool isPersistent)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[NetworkWorldItem] InitializeServer는 서버에서만 호출해야 함");
            return;
        }

        if (item == null)
        {
            Debug.LogWarning("[NetworkWorldItem] InitializeServer item null");
            return;
        }

        itemID.Value = item.itemID;
        quantity.Value = Mathf.Max(1, qty);
        persistent.Value = isPersistent;

        // 서버 로컬 인스턴스 비주얼도 즉시 반영
        if (itemID.Value > 0)
            TryApplyOrDefer();
    }

    private void OnItemChanged(int _, int newValue)
    {
        if (newValue <= 0) return;
        TryApplyOrDefer();
    }

    private void OnQuantityChanged(int _, int __)
    {
        if (itemID.Value <= 0) return;
        TryApplyOrDefer();
    }

    private void OnPersistentChanged(bool _, bool __)
    {
        if (itemID.Value <= 0) return;
        TryApplyOrDefer();
    }

    private void TryApplyOrDefer()
    {
        if (worldItem == null) return;

        if (ItemDatabase.I == null || !ItemDatabase.I.IsInitialized)
        {
            if (!pendingApply)
            {
                pendingApply = true;
                WaitAndApply().Forget();
            }
            return;
        }

        pendingApply = false;
        ApplyToWorldItem();
    }

    private async UniTaskVoid WaitAndApply()
    {
        await UniTask.WaitUntil(() => ItemDatabase.I != null && ItemDatabase.I.IsInitialized);

        if (this == null || !isActiveAndEnabled) return;
        ApplyToWorldItem();
    }

    private void ApplyToWorldItem()
    {
        if (worldItem == null || ItemDatabase.I == null || !ItemDatabase.I.IsInitialized)
        {
            Debug.LogWarning($"[NetworkWorldItem] Apply 실패 - worldItem:{worldItem != null}, db:{ItemDatabase.I != null}, init:{(ItemDatabase.I != null && ItemDatabase.I.IsInitialized)}");
            return;
        }

        if (itemID.Value <= 0)
        {
            Debug.LogWarning("[NetworkWorldItem] itemID가 0이라 ApplyToWorldItem 중단");
            return;
        }

        var data = ItemDatabase.I.GetItem(itemID.Value);
        if (data == null)
        {
            Debug.LogWarning($"[NetworkWorldItem] itemID {itemID.Value} 데이터 못 찾음");
            return;
        }

        Debug.Log($"[NetworkWorldItem] ApplyToWorldItem: {data.itemName}, qty={quantity.Value}, persistent={persistent.Value}");

        worldItem.Initialize(data, !persistent.Value);
        worldItem.SetQuantity(quantity.Value);
        worldItem.SetPersistent(persistent.Value);
    }

    public void TryRequestPickup(PlayerController player)
    {
        if (player == null) return;
        RequestPickupRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestPickupRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        Debug.Log($"[Pickup] RPC 도착 clientId={clientId}, IsServer={IsServer}, IsSpawned={IsSpawned}");

        if (!IsServer || !IsSpawned)
        {
            Debug.LogWarning("[Pickup] 서버 아님 또는 스폰 안됨");
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            Debug.LogWarning("[Pickup] ConnectedClients에서 client 못 찾음");
            return;
        }

        var playerObj = client.PlayerObject;
        if (playerObj == null)
        {
            Debug.LogWarning("[Pickup] playerObj null");
            return;
        }

        float dist = Vector3.Distance(playerObj.transform.position, transform.position);
        Debug.Log($"[Pickup] 거리={dist}");

        if (dist > 3f)
        {
            Debug.LogWarning("[Pickup] 거리 초과");
            return;
        }

        var player = playerObj.GetComponent<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[Pickup] PlayerController 없음");
            return;
        }

        if (player.Inventory == null)
        {
            Debug.LogWarning("[Pickup] player.Inventory null");
            return;
        }

        var itemData = ItemDatabase.I.GetItem(itemID.Value);
        if (itemData == null)
        {
            Debug.LogWarning($"[Pickup] itemData null, itemID={itemID.Value}");
            return;
        }

        int addedCount = player.Inventory.AddItemFromWorld(itemData.itemID, quantity.Value);
        int overflow = quantity.Value - addedCount;

        if (addedCount > 0 && NetworkQuestManager.I != null)
        {
            NetworkQuestManager.I.AddCollectProgressServer(itemData.itemID, addedCount);
        }

        // 습득한 클라이언트에게만 UI 피드백
        if (addedCount > 0)
        {
            ShowPickupFeedbackRpc(
                itemData.itemID,
                addedCount,
                overflow,
                RpcTarget.Single(clientId, RpcTargetUse.Temp)
            );
        }

        Debug.Log($"[Pickup] addedCount={addedCount}, overflow={overflow}");

        if (addedCount <= 0)
        {
            Debug.LogWarning("[Pickup] 인벤토리에 추가 실패");
            return;
        }

        if (addedCount < quantity.Value)
        {
            quantity.Value -= addedCount;
            ApplyToWorldItem();
            return;
        }

        Debug.Log("[Pickup] 전량 습득 성공, Despawn");
        NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowPickupFeedbackRpc(int itemId, int added, int overflow, RpcParams rpcParams = default)
    {
        var inventory = FindFirstObjectByType<GameInventory>();
        if (inventory == null) return;

        inventory.NotifyPickupFeedback(itemId, added, overflow);
    }
}