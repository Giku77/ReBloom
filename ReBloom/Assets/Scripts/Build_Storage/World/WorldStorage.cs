using UnityEngine;

public class WorldStorage : WorldItemContainerBase
{
    [Header("Storage References")]
    [SerializeField] private StorageData storageDataRef;

    private StorageData storageData;
    private static StorageUI sharedStorageUI;

    private string storageID;

    protected override IItemContainer Container => storageData;
    public override bool CanInteract() => storageData != null;

    protected override void Awake()
    {
        base.Awake();

        // 고유 ID 생성
        storageID = $"{gameObject.name}_{GetInstanceID()}";

        if (storageDataRef != null)
        {
            // 데이터 인스턴스 생성
            storageData = Instantiate(storageDataRef);
            storageData.name = $"StorageData_{storageID}";

            Debug.Log($"[WorldStorage] 창고 생성: {storageID}");
            Debug.Log($"→ Data Instance ID: {storageData.GetInstanceID()}");
        }
        else
        {
            Debug.LogError($"[WorldStorage] {gameObject.name}: StorageDataRef 미할당!");
        }

        // StorageUI 찾기 (싱글톤)
        if (sharedStorageUI == null)
        {
            sharedStorageUI = FindFirstObjectByType<StorageUI>();

            if (sharedStorageUI != null)
            {
                Debug.Log($"[WorldStorage] StorageUI 찾음: {sharedStorageUI.name}");
            }
            else
            {
                Debug.LogError("[WorldStorage] StorageUI를 찾을 수 없습니다!");
            }
        }
    }

    public override void Interact(PlayerController player)
    {
        if (storageData == null)
        {
            Debug.LogError($"[WorldStorage] {storageID}: 데이터 없음!");
            return;
        }

        Debug.Log($"[WorldStorage] 상호작용: {storageID}");
        OpenStorageUI();
    }

    private void OpenStorageUI()
    {
        if (sharedStorageUI == null)
        {
            Debug.LogError("[WorldStorage] StorageUI 없음!");
            return;
        }

        Debug.Log($"[WorldStorage] UI 열기 - {gameObject.name}");

        // 1. 데이터 설정
        sharedStorageUI.Initialize(storageData, this);

        // 2. DragDropManager에 등록
        DragDropManager.I.SetCurrentStorage(this);

        // 3. UI 열기
        sharedStorageUI.Toggle();
    }

    public void AddItem(ItemBase item, int quantity)
    {
        if (storageData != null)
        {
            storageData.AddItem(item.itemID, quantity);
        }
    }

    public StorageData GetStorageData() => storageData;

    public string GetStorageUID() => storageID;
}