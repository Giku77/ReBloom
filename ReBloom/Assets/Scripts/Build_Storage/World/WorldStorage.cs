using UnityEngine;

public class WorldStorage : WorldItemContainerBase
{
    [Header("Storage References")]
    [SerializeField] private StorageData storageDataRef;
    [SerializeField] private StorageUI storageUI;

    private StorageData storageData;

    protected override IItemContainer Container => storageData;

    public override bool CanInteract() => storageData != null;

    protected override void Awake()
    {
        base.Awake();

        if (storageDataRef != null)
        {
            // 런타임 인스턴스 생성
            storageData = Instantiate(storageDataRef);

            //StorageUI에 인스턴스 전달
            if (storageUI != null)
            {
                storageUI.Initialize(storageData, this);
            }
        }
    }

    public override void Interact(PlayerController player)
    {
        if (storageData == null)
        {
            Debug.LogError("[WorldStorage] 데이터가 없습니다!");
            return;
        }

        OpenStorageUI();
    }

    private void OpenStorageUI()
    {
        Debug.Log("[WorldStorage] 창고 UI 열기");

        if (storageUI != null)
        {
            storageUI.gameObject.SetActive(true);
        }
    }

    protected override void OnTransferComplete()
    {
        base.OnTransferComplete();
        // 창고는 비워져도 제거 안 함
    }

    public void AddItem(ItemBase item, int quantity)
    {
        if (storageData != null)
        {
            storageData.AddItem(item.itemID, quantity);
        }
    }

    public StorageData GetStorageData() => storageData;
}