using UnityEngine;

public class WorldStorage : WorldItemContainerBase
{
    [Header("Storage References")]
    [SerializeField] private StorageData storageDataRef;
    private StorageData storageData;

    protected override IItemContainer Container => storageData;

    // 창고는 비어있어도 상호작용 가능
    public override bool CanInteract() => storageData != null;

    protected override void Awake()
    {
        base.Awake();

        if (storageDataRef != null)
        {
            storageData = Instantiate(storageDataRef);
        }
    }

    // 창고는 즉시 회수가 아니라 UI 열기
    public override void Interact(PlayerController player)
    {
        if (storageData == null)
        {
            Debug.LogError("[WorldStorage] 데이터가 없습니다!");
            return;
        }

        // 창고 UI 열기 (나중에 구현)
        OpenStorageUI();
    }

    private void OpenStorageUI()
    {
        Debug.Log("[WorldStorage] 창고 UI 열기");
        // TODO: UIManager.Instance.OpenStorage(storageData, playerInventory);
    }

    protected override void OnTransferComplete()
    {
        base.OnTransferComplete();
        // 창고는 비워져도 제거 안 함
    }
}