using Cysharp.Threading.Tasks;
using UnityEngine;

public class WorldStorage : WorldItemContainerBase
{
    [Header("Storage References")]
    [SerializeField] private StorageData storageDataRef;
    [SerializeField] private NetworkStorageContainer networkStorage;

    private StorageData storageData;
    private static StorageUI sharedStorageUI;

    private string storageID;

    protected override IItemContainer Container => storageData;
    public override bool CanInteract() => storageData != null;

    [SerializeField] private string containerGuid;

    public string ContainerGuid => containerGuid;

    public void SetContainerGuid(string guid)
    {
        containerGuid = guid;
    }

    protected override void Awake()
    {
        base.Awake();

        storageData = Instantiate(storageDataRef);

        if (networkStorage == null)
            networkStorage = GetComponent<NetworkStorageContainer>();

        if (networkStorage != null)
            networkStorage.BindMirror(storageData);

        if (string.IsNullOrEmpty(containerGuid))
        {
            var id = GetComponent<SaveableEntity>();
            if (id != null && !string.IsNullOrEmpty(id.PersistentId))
                containerGuid = $"container:{id.PersistentId}";
        }

        if (sharedStorageUI == null)
        {
            sharedStorageUI = FindFirstObjectByType<StorageUI>();

            if (sharedStorageUI != null)
                Debug.Log($"[WorldStorage] StorageUI 찾음: {sharedStorageUI.name}");
            else
                Debug.LogError("[WorldStorage] StorageUI를 찾을 수 없습니다!");
        }
    }

    public override void Interact(PlayerController player)
    {
        if (storageData == null)
            return;

        player.SetCurrentStorage(this);
        OpenStorageUI();
    }

    private void OpenStorageUI()
    {
        if (sharedStorageUI == null)
        {
            Debug.LogError("[WorldStorage] StorageUI 없음!");
            return;
        }

        sharedStorageUI.Initialize(storageData, this);
        DragDropManager.I.SetCurrentStorage(this);

        if (!sharedStorageUI.IsOpen)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowUI(sharedStorageUI.Type);
            else
                sharedStorageUI.Show();
        }
        else
        {
            sharedStorageUI.RefreshUI();
        }
    }

    public void CloseUI()
    {
        if (sharedStorageUI != null)
            sharedStorageUI.Toggle();
    }

    public void AddItem(ItemBase item, int quantity)
    {
        if (item == null || quantity <= 0)
            return;

        if (networkStorage != null)
            networkStorage.ServerTryAddItem(item.itemID, quantity);
        else if (storageData != null)
            storageData.AddItem(item.itemID, quantity);
    }

    public bool RemoveItem(int itemID, int quantity)
    {
        if (quantity <= 0)
            return false;

        if (networkStorage != null)
            return networkStorage.ServerTryRemoveItem(itemID, quantity);

        return storageData != null && storageData.TryRemoveItem(itemID, quantity);
    }

    public bool RequestDepositFromInventory(int itemID, int quantity)
    {
        if (quantity <= 0)
            return false;

        if (networkStorage != null)
            return networkStorage.RequestDepositFromLocalPlayer(itemID, quantity);

        if (playerInventory == null || storageData == null)
            return false;

        return playerInventory.TransferTo(storageData, itemID, quantity);
    }

    public bool RequestWithdrawToInventory(int itemID, int quantity)
    {
        if (quantity <= 0)
            return false;

        if (networkStorage != null)
            return networkStorage.RequestWithdrawToLocalPlayer(itemID, quantity);

        if (playerInventory == null || storageData == null)
            return false;

        return storageData.TransferTo(playerInventory.Container, itemID, quantity);
    }

    public bool RequestDepositAllFromInventory()
    {
        if (networkStorage != null)
            return networkStorage.RequestDepositAllFromLocalPlayer();

        if (playerInventory == null || storageData == null)
            return false;

        return playerInventory.DepositAllTo(storageData);
    }

    public bool RequestWithdrawAllToInventory()
    {
        if (networkStorage != null)
            return networkStorage.RequestWithdrawAllToLocalPlayer();

        if (playerInventory == null || storageData == null)
            return false;

        return playerInventory.WithdrawAllFrom(storageData);
    }

    public bool RequestDropToWorld(ItemBase item, int quantity)
    {
        if (item == null || quantity <= 0)
            return false;

        if (networkStorage != null)
            return networkStorage.RequestDropToWorldFromLocalPlayer(item.itemID, quantity);

        if (storageData == null || !storageData.TryRemoveItem(item.itemID, quantity))
            return false;

        DropToWorldLocal(item, quantity).Forget();
        return true;
    }

    public StorageData GetStorageData() => storageData;

    public string GetStorageUID() => storageID;

    public void LoadFromSnapshot(ContainerSaveDTO dto)
    {
        if (storageData == null || dto == null)
            return;

        if (networkStorage != null)
            networkStorage.ServerClear();
        else
            storageData.Clear();

        foreach (var item in dto.items)
        {
            if (item == null || item.itemId <= 0 || item.amount <= 0)
                continue;

            if (networkStorage != null)
                networkStorage.ServerTryAddItem(item.itemId, item.amount);
            else
                storageData.TryAddItem(item.itemId, item.amount);
        }
    }

    private async UniTaskVoid DropToWorldLocal(ItemBase item, int quantity)
    {
        var itemSpawner = FindFirstObjectByType<ItemSpawner>();
        if (itemSpawner == null || playerTransform == null)
            return;

        Vector3 dropPosition = playerTransform.position + playerTransform.forward * 1.25f + Vector3.up * 0.75f;
        await itemSpawner.DropItemWithQuantity(item, dropPosition, quantity);
        storageData.NotifyStorageChanged();
    }
}
