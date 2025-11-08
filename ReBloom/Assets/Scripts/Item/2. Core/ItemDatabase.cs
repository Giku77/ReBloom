using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 데이터베이스 매니저 (Singleton)
/// BG Database의 모든 아이템을 로드하고 관리
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    #region Singleton
    private static ItemDatabase instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ItemDatabase>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[ItemDatabase]");
                    instance = go.AddComponent<ItemDatabase>();
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("BG Database 설정")]
    [SerializeField] private BGRepo repository = BGRepo.I;

    [Header("테이블 이름 (BG Database에서 확인)")]
    [SerializeField] private string consumableTableName = "ConsumeItemTable";
    [SerializeField] private string protectiveTableName = "ProtectiveItemTable";
    [SerializeField] private string toolTableName = "ToolItemTable";

    // 아이템 캐시 (ID로 빠른 접근)
    private Dictionary<int, ItemBase> itemCache = new Dictionary<int, ItemBase>();

    // 초기화 완료 플래그
    public bool IsInitialized { get; private set; } = false;

    private void Awake()
    {
        // Singleton 처리
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 초기화
        Initialize();
    }

    /// <summary>
    /// 데이터베이스 초기화
    /// </summary>
    private void Initialize()
    {
        if (repository == null)
        {
            Debug.LogError("[ItemDatabase] BGRepo가 할당되지 않았습니다!");
            return;
        }

        Debug.Log("[ItemDatabase] 아이템 데이터 로드 시작...");

        // 각 테이블 로드
        LoadConsumableItems();
        LoadProtectiveItems();
        LoadToolItems();

        IsInitialized = true;
        Debug.Log($"[ItemDatabase] 초기화 완료 - 총 {itemCache.Count}개 아이템 로드됨");
    }

    /// <summary>
    /// 소비 아이템 테이블 로드
    /// </summary>
    private void LoadConsumableItems()
    {
        var meta = repository.GetMeta(consumableTableName);
        if (meta == null)
        {
            Debug.LogError($"[ItemDatabase] 테이블을 찾을 수 없음: {consumableTableName}");
            return;
        }

        int count = 0;
        
        while (meta.GetEntity(count) != null)
        {
            var entity = meta.GetEntity(count);
            var item = ScriptableObject.CreateInstance<ConsumableItemData>();
            item.Initialize(entity);

            if (itemCache.ContainsKey(item.itemID))
            {
                Debug.LogWarning($"[ItemDatabase] 중복된 아이템 ID: {item.itemID}");
                continue;
            }

            itemCache[item.itemID] = item;
            count++;
        }

        Debug.Log($"[ItemDatabase] 소비 아이템 {count}개 로드 완료");
    }

    /// <summary>
    /// 보호구 아이템 테이블 로드
    /// </summary>
    private void LoadProtectiveItems()
    {
        var meta = repository.GetMeta(protectiveTableName);
        if (meta == null)
        {
            Debug.LogWarning($"[ItemDatabase] 테이블을 찾을 수 없음: {protectiveTableName}");
            return;
        }

        int count = 0;
        while (meta.GetEntity(count) != null)
        {
            var entity = meta.GetEntity(count);
            var item = ScriptableObject.CreateInstance<ProtectiveItemData>();
            item.Initialize(entity);
            itemCache[item.itemID] = item;
            count++;
        }

        Debug.Log($"[ItemDatabase] 보호구 {count}개 로드 완료");
    }

    /// <summary>
    /// 도구 아이템 테이블 로드 (TODO)
    /// </summary>
    private void LoadToolItems()
    {
        // TODO: 도구 아이템 구현 시 추가
    }

    /// <summary>
    /// ID로 아이템 가져오기
    /// </summary>
    public ItemBase GetItem(int itemID)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[ItemDatabase] 아직 초기화되지 않았습니다!");
            return null;
        }

        if (itemCache.TryGetValue(itemID, out ItemBase item))
        {
            return item;
        }

        Debug.LogWarning($"[ItemDatabase] 아이템을 찾을 수 없음: ID {itemID}");
        return null;
    }

    /// <summary>
    /// 카테고리별 아이템 목록
    /// </summary>
    public List<ItemBase> GetItemsByCategory(InventorySlotType category)
    {
        List<ItemBase> result = new List<ItemBase>();

        foreach (var item in itemCache.Values)
        {
            if (item.slotType == category)
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// 티어별 아이템 목록
    /// </summary>
    public List<ItemBase> GetItemsByTier(int tier)
    {
        List<ItemBase> result = new List<ItemBase>();

        foreach (var item in itemCache.Values)
        {
            if (item.tier == tier)
            {
                result.Add(item);
            }
        }

        return result;
    }

    #region 디버그 메서드

    /// <summary>
    /// 모든 아이템 출력
    /// </summary>
    [ContextMenu("Debug/Print All Items")]
    public void DebugPrintAllItems()
    {
        Debug.Log("========== 아이템 데이터베이스 ==========");

        foreach (var item in itemCache.Values)
        {
            Debug.Log($"[{item.itemID}] {item.itemName} (티어{item.tier}, {item.slotType})");
        }

        Debug.Log($"총 {itemCache.Count}개 아이템");
        Debug.Log("=====================================");
    }

    /// <summary>
    /// 특정 아이템 상세 정보
    /// </summary>
    [ContextMenu("Debug/Print Item Details")]
    public void DebugPrintItemDetails()
    {
        // 테스트용: ID 4001001 (통조림)
        ItemBase item = GetItem(4001001);

        if (item != null)
        {
            Debug.Log($"=== {item.itemName} ===");
            Debug.Log($"ID: {item.itemID}");
            Debug.Log($"설명: {item.description}");
            Debug.Log($"최대 스택: {item.maxCount}");
            Debug.Log($"퀵슬롯: {item.canQuickSlot}");
        }
    }

    #endregion
}