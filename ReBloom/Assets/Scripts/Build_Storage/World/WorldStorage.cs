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
            
            // StorageUI에 인스턴스 전달
            if (storageUI != null)
            {
                storageUI.Initialize(storageData, this);
            }
            else
            {
                Debug.LogError("[WorldStorage] StorageUI가 할당되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogError("[WorldStorage] StorageDataRef가 할당되지 않았습니다!");
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
            DragDropManager.I.SetCurrentStorage(this);
            storageUI.Toggle(); // 토글 방식으로 변경
        }
        else
        {
            Debug.LogError("[WorldStorage] StorageUI를 찾을 수 없습니다!");
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