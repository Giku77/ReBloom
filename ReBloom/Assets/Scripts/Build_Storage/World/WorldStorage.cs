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

    [SerializeField] private string containerGuid; // 고정 키

    public string ContainerGuid => containerGuid;

    public void SetContainerGuid(string guid)
    {
        containerGuid = guid;
    }

    protected override void Awake()
    {
        base.Awake();

        // data instantiate
        storageData = Instantiate(storageDataRef);

        // guid 자동 세팅
        if (string.IsNullOrEmpty(containerGuid))
        {
            var id = GetComponent<SaveableEntity>();
            if (id != null && !string.IsNullOrEmpty(id.PersistentId))
            {
                containerGuid = $"container:{id.PersistentId}";
            }
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
            return;
        }

        // 1. 플레이어에게 현재 창고 알림 (거리 체크용)
        player.SetCurrentStorage(this);

        // 2. UI 열기
        OpenStorageUI();
    }

    private void OpenStorageUI()
    {
        if (sharedStorageUI == null)
        {
            Debug.LogError("[WorldStorage] StorageUI 없음!");
            return;
        }

        // 1. 데이터 설정
        sharedStorageUI.Initialize(storageData, this);

        // 2. DragDropManager에 등록
        DragDropManager.I.SetCurrentStorage(this);

        // 3. UI 열기
        Debug.Log($"[WorldStorage] IsOpen: {sharedStorageUI.IsOpen}, Type: {sharedStorageUI.Type}");

        if (!sharedStorageUI.IsOpen)
        {
            // UIManager 확인
            if (UIManager.Instance != null)
            {
                Debug.Log("[WorldStorage] UIManager.ShowUI 호출");
                UIManager.Instance.ShowUI(sharedStorageUI.Type);
            }
            else
            {
                Debug.LogError("[WorldStorage] UIManager.Instance가 null!");
                sharedStorageUI.Show();  // fallback
            }
        }
        else
        {
            sharedStorageUI.RefreshUI();
        }
    }

    /// <summary>
    /// 외부에서 UI 닫기 (PlayerController.CheckStorageDistance에서 호출)
    /// </summary>
    public void CloseUI()
    {
        if (sharedStorageUI != null)
        {
            sharedStorageUI.Toggle();
        }
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