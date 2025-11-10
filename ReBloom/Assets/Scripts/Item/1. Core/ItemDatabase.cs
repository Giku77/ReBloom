using BansheeGz.BGDatabase;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 아이템 데이터베이스 매니저 (Singleton)
/// BG Database의 모든 아이템 테이블을 로드하고 관리
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    #region Singleton
    private static ItemDatabase instance;
    public static ItemDatabase I
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

    private BGRepo repository;

    private Dictionary<int, ItemBase> itemCache = new Dictionary<int, ItemBase>();

    // 초기화 완료 플래그
    public bool IsInitialized { get; private set; } = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    /// <summary>
    /// BG Database 테이블 이름 배열
    /// ItemTableType 열거형과 순서가 일치해야 함
    /// </summary>
    private string[] itemTableNames = new string[]
    {
        "Item_Consumable",  // 소비 아이템
        "Item_Equip",       // 보호구
        "Item_Tool",        // 도구
        "Item_Etc"         // 기타 (종자, 자원, 재료)
    };

    /// <summary>
    /// 데이터베이스 초기화
    /// </summary>
    private void Initialize()
    {
        repository = BGRepo.I;

        if (repository == null)
        {
            Debug.LogError("[ItemDatabase] BGRepo가 할당되지 않았습니다! BG Database가 씬에 있는지 확인하세요.");
            return;
        }

        Debug.Log("[ItemDatabase] 아이템 데이터 로드 시작...");

        // 각 테이블에서 아이템 로드
        for (int i = 0; i < itemTableNames.Length; i++)
        {
            string tableName = itemTableNames[i];
            ItemTableType tableType = (ItemTableType)i;

            LoadItemsFromTable(tableName, tableType);
        }

        IsInitialized = true;
        Debug.Log($"[ItemDatabase] 초기화 완료 - 총 {itemCache.Count}개 아이템 로드됨");

        // 통계 출력
        PrintLoadStatistics();
    }

    /// <summary>
    /// 특정 테이블에서 아이템 로드 - Factory에게 생성 위임
    /// </summary>
    private void LoadItemsFromTable(string tableName, ItemTableType tableType)
    {
        var meta = repository.GetMeta(tableName);

        if (meta == null)
        {
            Debug.LogWarning($"[ItemDatabase] 테이블을 찾을 수 없음: {tableName}");
            return;
        }

        int loadedCount = 0;
        int duplicateCount = 0;

        for (int i = 0; i < meta.CountEntities; i++)
        {
            var entity = meta.GetEntity(i);

            // Factory에게 아이템 생성 요청
            ItemBase item = ItemFactory.CreateItem(entity, tableType);

            if (item == null)
            {
                Debug.LogWarning($"[ItemDatabase] 아이템 생성 실패: {tableName} 인덱스 {i}");
                continue;
            }

            // 중복 ID 체크
            if (itemCache.ContainsKey(item.itemID))
            {
                Debug.LogWarning($"[ItemDatabase] 중복된 아이템 ID: {item.itemID} ({item.itemName})");
                duplicateCount++;
                continue;
            }

            // 캐시에 추가
            itemCache[item.itemID] = item;
            loadedCount++;
        }

        string statusMsg = $"[ItemDatabase] {tableName} 로드 완료: {loadedCount}개 성공";
        if (duplicateCount > 0)
        {
            statusMsg += $", {duplicateCount}개 중복";
        }
        Debug.Log(statusMsg);
    }

    #region 아이템 조회 메서드

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
    /// 슬롯 타입별 아이템 목록
    /// </summary>
    public List<ItemBase> GetItemsBySlotType(InventorySlotType slotType)
    {
        return itemCache.Values
            .Where(item => item.slotType == slotType)
            .ToList();
    }

    /// <summary>
    /// 티어별 아이템 목록
    /// </summary>
    public List<ItemBase> GetItemsByTier(int tier)
    {
        return itemCache.Values
            .Where(item => item.tier == tier)
            .ToList();
    }

    /// <summary>@
    /// 테이블 타입별 아이템 가져오기
    /// </summary>
    public List<ItemBase> GetItemsByTableType(ItemTableType tableType)
    {
        return itemCache.Values
            .Where(item => ItemIDParser.GetTableType(item.itemID) == tableType)
            .ToList();
    }

    /// <summary>
    /// 모든 아이템 가져오기 (디버그용)
    /// </summary>
    public List<ItemBase> GetAllItems()
    {
        return new List<ItemBase>(itemCache.Values);
    }

    /// <summary>
    /// 아이템 존재 여부 확인
    /// </summary>
    public bool HasItem(int itemID)
    {
        return itemCache.ContainsKey(itemID);
    }

    #endregion

    #region 통계 및 디버그 메서드

    /// <summary>
    /// 로드 통계 출력
    /// </summary>
    private void PrintLoadStatistics()
    {
        Debug.Log("========== 아이템 로드 통계 ==========");

        // 테이블별 카운트
        foreach (ItemTableType tableType in System.Enum.GetValues(typeof(ItemTableType)))
        {
            int count = GetItemsByTableType(tableType).Count;
            Debug.Log($"{GetTableName(tableType)}: {count}개");
        }

        // 티어별 카운트
        for (int tier = 1; tier <= 3; tier++)
        {
            int count = GetItemsByTier(tier).Count;
            Debug.Log($"티어 {tier}: {count}개");
        }

        Debug.Log($"총 아이템: {itemCache.Count}개");
        Debug.Log("=====================================");
    }

    /// <summary>
    /// 모든 아이템 출력
    /// </summary>
    [ContextMenu("Debug/Print All Items")]
    public void DebugPrintAllItems()
    {
        Debug.Log("========== 아이템 데이터베이스 ==========");

        foreach (var item in itemCache.Values.OrderBy(i => i.itemID))
        {
            string tierName = item.tier switch
            {
                1 => "일반",
                2 => "희귀",
                3 => "영웅",
                _ => "???"
            };

            Debug.Log($"[{item.itemID}] {item.itemName} ({tierName}, {item.slotType})");
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
        // 테스트용: 각 테이블에서 첫 번째 아이템 출력
        PrintItemDetail(4001001); // 소비 아이템
        PrintItemDetail(4101001); // 보호구
        PrintItemDetail(4201001); // 도구
        PrintItemDetail(2001001); // 기타 아이템
    }

    /// <summary>
    /// 개별 아이템 상세 정보 출력
    /// </summary>
    private void PrintItemDetail(int itemID)
    {
        ItemBase item = GetItem(itemID);

        if (item == null)
        {
            Debug.LogWarning($"아이템 ID {itemID}를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"=== {item.itemName} ===");
        Debug.Log($"ID: {item.itemID}");
        Debug.Log($"설명: {item.description}");
        Debug.Log($"슬롯: {item.slotType}");
        Debug.Log($"티어: {item.tier}");
        Debug.Log($"최대 스택: {item.maxCount}");
        Debug.Log($"퀵슬롯: {item.canQuickSlot}");
        Debug.Log($"버리기: {item.canDiscard}");
        Debug.Log($"창고 저장: {item.canStorage}");
        Debug.Log("====================");
    }

    /// <summary>
    /// 테이블 이름 가져오기
    /// </summary>
    private string GetTableName(ItemTableType type)
    {
        return type switch
        {
            ItemTableType.Consumable => "소비 아이템",
            ItemTableType.Protective => "보호구",
            ItemTableType.Tool => "도구",
            ItemTableType.Misc => "기타",
            _ => "알 수 없음"
        };
    }

    #endregion

    /// <summary>
    /// Unity 이벤트 - 시작 시 디버그 정보 출력
    /// </summary>
    private void Start()
    {
        if (IsInitialized)
        {
            // 테스트용 상세 정보 출력 (개발 중에만 활성화)
#if UNITY_EDITOR
            // DebugPrintItemDetails();
#endif
        }
    }
}