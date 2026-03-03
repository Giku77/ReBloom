using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerItemNetworkBridge : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("Drop Settings")]
    [SerializeField] private float dropForwardOffset = 1.25f;
    [SerializeField] private float dropUpOffset = 0.75f;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
    }

    public void RequestDropFromInventory(ItemBase item, int quantity)
    {
        if (item == null || quantity <= 0) return;

        // 싱글플레이 fallback
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            LocalDrop(item, quantity).Forget();
            return;
        }

        if (!IsOwner) return;

        RequestDropInventoryRpc(item.itemID, quantity);
    }

    private async UniTaskVoid LocalDrop(ItemBase item, int quantity)
    {
        if (playerController == null || playerController.Inventory == null || itemSpawner == null)
            return;

        if (!playerController.Inventory.TryRemoveItem(item.itemID, quantity))
            return;

        Vector3 dropPos = transform.position + transform.forward * dropForwardOffset + Vector3.up * dropUpOffset;
        await itemSpawner.DropItemWithQuantity(item, dropPos, quantity);
    }

    [Rpc(SendTo.Server)]
    private void RequestDropInventoryRpc(int itemID, int quantity, RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        HandleDropInventoryServer(itemID, quantity, rpcParams.Receive.SenderClientId).Forget();
    }

    private async UniTaskVoid HandleDropInventoryServer(int itemID, int quantity, ulong senderClientId)
    {
        if (quantity <= 0) return;

        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();

        if (itemSpawner == null)
        {
            Debug.LogError("[PlayerItemNetworkBridge] ItemSpawner 없음");
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
            return;

        var playerObj = client.PlayerObject;
        if (playerObj == null) return;

        var controller = playerObj.GetComponent<PlayerController>();
        if (controller == null || controller.Inventory == null) return;

        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null) return;

        // 서버에서 실제 차감
        bool removed = controller.Inventory.TryRemoveItem(itemID, quantity);
        if (!removed) return;

        Vector3 dropPos = playerObj.transform.position
                        + playerObj.transform.forward * dropForwardOffset
                        + Vector3.up * dropUpOffset;

        await itemSpawner.DropItemWithQuantity(item, dropPos, quantity);
    }
}