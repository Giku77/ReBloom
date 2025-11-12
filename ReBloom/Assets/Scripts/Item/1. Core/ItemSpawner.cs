using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using static BansheeGz.BGDatabase.BGJsonRepoModel;

public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform itemParent;

    [Header("Object Pool Settings")]
    [SerializeField] private int defaultPoolSize = 20;
    [SerializeField] private int maxPoolSize = 200; // 디버그용 대량 생성 고려
    [SerializeField] private bool collectionCheck = true;

    [Header("Batch Spawn Settings")]
    [SerializeField] private float batchSpawnRadius = 3f; // 대량 생성 시 퍼지는 반경
    [SerializeField] private int maxSpawnPerFrame = 10;   // 프레임당 최대 생성 수

    // 아이템 ID별 오브젝트 풀
    private Dictionary<int, ObjectPool<GameObject>> itemPools = new Dictionary<int, ObjectPool<GameObject>>();

    // 프리팹 캐시
    private Dictionary<int, GameObject> prefabCache = new Dictionary<int, GameObject>();

    // 디버그용 통계
    public PoolStatistics Statistics { get; private set; } = new PoolStatistics();

    #region 단일 아이템 스폰
    public async Task<GameObject> SpawnItemInWorld(int itemID, Vector3 position)
    {
        ItemBase itemData = ItemDatabase.I.GetItem(itemID);
        if (itemData == null)
        {
            Debug.LogError($"[ItemSpawner] 아이템 데이터 없음: {itemID}");
            return null;
        }

        return await SpawnItemInWorld(itemData, position);
    }

    public async Task<GameObject> SpawnItemInWorld(ItemBase itemData, Vector3 position)
    {
        if (itemData == null)
        {
            Debug.LogError($"[ItemSpawner] 아이템 데이터가 null입니다!");
            return null;
        }

        int itemID = itemData.itemID;

        // 풀이 없으면 생성
        if (!itemPools.ContainsKey(itemID))
        {
            await CreatePoolForItem(itemData);
        }

        // 풀에서 가져오기
        ObjectPool<GameObject> pool = itemPools[itemID];
        GameObject itemObj = pool.Get();

        itemObj.transform.position = position;
        itemObj.transform.rotation = Quaternion.identity;

        var worldItem = itemObj.GetComponent<WorldItem>();
        worldItem?.Initialize(itemData);

        return itemObj;
    }

    public async Task<GameObject> DropItem(ItemBase itemData, Vector3 position, Vector3 force)
    {
        GameObject itemObj = await SpawnItemInWorld(itemData, position);

        if (itemObj != null && itemObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(force, ForceMode.Impulse);
            rb.angularVelocity = Random.insideUnitSphere * 2f;
        }

        return itemObj;
    }
    #endregion

    #region 대량 아이템 스폰 (디버그용)
    /// <summary>
    /// 여러 개의 아이템을 한 번에 스폰 (디버그/크리에이티브 모드용)
    /// </summary>
    /// <param name="itemData">아이템 데이터</param>
    /// <param name="centerPosition">중심 위치</param>
    /// <param name="count">생성 개수</param>
    /// <param name="scatterRadius">퍼지는 반경</param>
    public async Task<List<GameObject>> SpawnItemBatch(ItemBase itemData, Vector3 centerPosition, int count, float scatterRadius = 0f)
    {
        if (itemData == null)
        {
            Debug.LogError("[ItemSpawner] 아이템 데이터가 null입니다!");
            return null;
        }

        // 스캐터 반경이 0이면 기본값 사용
        if (scatterRadius <= 0f)
        {
            scatterRadius = batchSpawnRadius;
        }

        List<GameObject> spawnedItems = new List<GameObject>();

        // 풀이 없으면 생성
        int itemID = itemData.itemID;
        if (!itemPools.ContainsKey(itemID))
        {
            await CreatePoolForItem(itemData);
        }

        // 프레임 분산 생성 (너무 많으면 여러 프레임에 나눠서)
        int spawnedThisFrame = 0;

        for (int i = 0; i < count; i++)
        {
            // 랜덤 위치 계산 (원형으로 퍼지기)
            Vector3 randomOffset = Random.insideUnitSphere * scatterRadius;
            randomOffset.y = 0; // Y축은 고정
            Vector3 spawnPosition = centerPosition + randomOffset;

            // 아이템 생성
            GameObject itemObj = await SpawnItemInWorld(itemData, spawnPosition);
            if (itemObj != null)
            {
                spawnedItems.Add(itemObj);

                // Rigidbody가 있으면 약간의 힘 적용 (자연스럽게 퍼지기)
                if (itemObj.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    // 중심에서 바깥쪽으로 밀어내기
                    Vector3 pushDirection = (spawnPosition - centerPosition).normalized;
                    rb.AddForce(pushDirection * Random.Range(1f, 3f), ForceMode.Impulse);
                    rb.angularVelocity = Random.insideUnitSphere * 2f;
                }
            }

            // 프레임당 생성 수 제한
            spawnedThisFrame++;
            if (spawnedThisFrame >= maxSpawnPerFrame)
            {
                spawnedThisFrame = 0;
                await Task.Yield(); // 다음 프레임으로 양보
            }
        }

        Debug.Log($"[ItemSpawner] 대량 생성 완료: {itemData.itemName} x{count}개");
        return spawnedItems;
    }

    /// <summary>
    /// 스택 아이템 드롭 (수량 설정 가능)
    /// </summary>
    public async Task<GameObject> DropItemWithQuantity(ItemBase itemData, Vector3 position, int quantity)
    {
        GameObject itemObj = await SpawnItemInWorld(itemData, position);

        if (itemObj != null)
        {
            // WorldItem에 수량 설정
            var worldItem = itemObj.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.SetQuantity(quantity);
            }

            // 물리 적용
            if (itemObj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.angularVelocity = Random.insideUnitSphere * 2f;
            }
        }

        return itemObj;
    }
    #endregion

    #region 오브젝트 풀 관리 (기존 코드)
    private async Task CreatePoolForItem(ItemBase itemData)
    {
        int itemID = itemData.itemID;

        GameObject prefab = await LoadOrGetCachedPrefab(itemData);
        if (prefab == null)
        {
            Debug.LogError($"[ItemSpawner] 프리팹 로드 실패: {itemData.itemName}");
            return;
        }

        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => CreatePooledItem(prefab, itemID),
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: collectionCheck,
            defaultCapacity: defaultPoolSize,
            maxSize: maxPoolSize
        );

        itemPools[itemID] = pool;

        // 통계 초기화
        Statistics.RegisterPool(itemID, itemData.itemName);

        Debug.Log($"[ItemSpawner] 오브젝트 풀 생성: {itemData.itemName} (ID: {itemID})");
    }

    private GameObject CreatePooledItem(GameObject prefab, int itemID)
    {
        GameObject obj = Instantiate(prefab, itemParent);
        obj.name = $"Item_{itemID}";
        obj.SetActive(false);

        PooledItem pooledItem = obj.GetComponent<PooledItem>();
        if (pooledItem == null)
        {
            pooledItem = obj.AddComponent<PooledItem>();
        }
        pooledItem.Initialize(this, itemID);

        // 통계 업데이트
        Statistics.IncrementCreated(itemID);

        return obj;
    }

    private void OnGetFromPool(GameObject obj)
    {
        obj.SetActive(true);

        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 통계 업데이트
        var pooledItem = obj.GetComponent<PooledItem>();
        if (pooledItem != null)
        {
            Statistics.IncrementGet(pooledItem.ItemID);
        }
    }

    private void OnReleaseToPool(GameObject obj)
    {
        obj.SetActive(false);

        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 통계 업데이트
        var pooledItem = obj.GetComponent<PooledItem>();
        if (pooledItem != null)
        {
            Statistics.IncrementRelease(pooledItem.ItemID);
        }
    }

    private void OnDestroyPoolObject(GameObject obj)
    {
        // 통계 업데이트
        var pooledItem = obj.GetComponent<PooledItem>();
        if (pooledItem != null)
        {
            Statistics.IncrementDestroyed(pooledItem.ItemID);
        }

        Destroy(obj);
    }

    public void ReturnToPool(GameObject itemObj, int itemID)
    {
        if (itemPools.ContainsKey(itemID))
        {
            itemPools[itemID].Release(itemObj);
        }
        else
        {
            Debug.LogWarning($"[ItemSpawner] 해당 ID의 풀이 없습니다: {itemID}. 파괴합니다.");
            Destroy(itemObj);
        }
    }
    #endregion

    #region 프리팹 로딩 (기존 코드)
    private async Task<GameObject> LoadOrGetCachedPrefab(ItemBase itemData)
    {
        int itemID = itemData.itemID;

        if (prefabCache.ContainsKey(itemID))
        {
            return prefabCache[itemID];
        }

        GameObject prefab = await LoadItemPrefabAsync(itemData);
        if (prefab != null)
        {
            prefabCache[itemID] = prefab;
        }

        return prefab;
    }

    private async Task<GameObject> LoadItemPrefabAsync(ItemBase itemData)
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(itemData.worldPrefabAddress);
            GameObject prefab = await handle.Task;
            return prefab;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemSpawner] 프리팹 로드 실패: {itemData.itemName}\n{e.Message}");
            return null;
        }
    }
    #endregion

    #region 유틸리티
    public async Task PreloadItemPool(int itemID, int count = 10)
    {
        ItemBase itemData = ItemDatabase.I.GetItem(itemID);
        if (itemData == null) return;

        if (!itemPools.ContainsKey(itemID))
        {
            await CreatePoolForItem(itemData);
        }

        ObjectPool<GameObject> pool = itemPools[itemID];
        List<GameObject> preloadedObjects = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            preloadedObjects.Add(pool.Get());
        }

        foreach (var obj in preloadedObjects)
        {
            pool.Release(obj);
        }

        Debug.Log($"[ItemSpawner] 아이템 프리로드 완료: ID {itemID}, {count}개");
    }

    public void ClearAllPools()
    {
        foreach (var pool in itemPools.Values)
        {
            pool.Clear();
        }

        itemPools.Clear();
        prefabCache.Clear();
    }
    #endregion

    #region 디버그 기능
    /// <summary>
    /// 특정 아이템 풀의 상태 조회
    /// </summary>
    public PoolInfo GetPoolInfo(int itemID)
    {
        if (!itemPools.ContainsKey(itemID))
            return null;

        var pool = itemPools[itemID];
        return new PoolInfo
        {
            ItemID = itemID,
            ItemName = ItemDatabase.I.GetItem(itemID)?.itemName ?? "Unknown",
            CountActive = pool.CountActive,
            CountInactive = pool.CountInactive,
            CountAll = pool.CountAll,
            IsCached = prefabCache.ContainsKey(itemID)
        };
    }

    /// <summary>
    /// 모든 풀의 상태 조회
    /// </summary>
    public List<PoolInfo> GetAllPoolInfo()
    {
        List<PoolInfo> infoList = new List<PoolInfo>();

        foreach (var kvp in itemPools)
        {
            infoList.Add(GetPoolInfo(kvp.Key));
        }

        return infoList;
    }

    /// <summary>
    /// 풀 강제 정리 (테스트용)
    /// </summary>
    [ContextMenu("Debug/Clear Specific Pool")]
    public void ClearPool(int itemID)
    {
        if (itemPools.ContainsKey(itemID))
        {
            itemPools[itemID].Clear();
            itemPools.Remove(itemID);
            prefabCache.Remove(itemID);

            Debug.Log($"[ItemSpawner] 풀 정리 완료: ID {itemID}");
        }
    }

    /// <summary>
    /// 통계 리셋 (테스트용)
    /// </summary>
    [ContextMenu("Debug/Reset Statistics")]
    public void ResetStatistics()
    {
        Statistics = new PoolStatistics();
        Debug.Log("[ItemSpawner] 통계 리셋 완료");
    }
    #endregion

    private void OnDestroy()
    {
        ClearAllPools();
    }

    #region 디버그 데이터 구조
    /// <summary>
    /// 풀 정보
    /// </summary>
    [System.Serializable]
    public class PoolInfo
    {
        public int ItemID;
        public string ItemName;
        public int CountActive;
        public int CountInactive;
        public int CountAll;
        public bool IsCached;
    }
}


/// <summary>
/// 풀 통계
/// </summary>
[System.Serializable]
public class PoolStatistics
{
    private Dictionary<int, PoolItemStats> stats = new Dictionary<int, PoolItemStats>();

    public void RegisterPool(int itemID, string itemName)
    {
        if (!stats.ContainsKey(itemID))
        {
            stats[itemID] = new PoolItemStats { ItemID = itemID, ItemName = itemName };
        }
    }

    public void IncrementCreated(int itemID) => GetOrCreate(itemID).TotalCreated++;
    public void IncrementGet(int itemID) => GetOrCreate(itemID).TotalGets++;
    public void IncrementRelease(int itemID) => GetOrCreate(itemID).TotalReleases++;
    public void IncrementDestroyed(int itemID) => GetOrCreate(itemID).TotalDestroyed++;

    private PoolItemStats GetOrCreate(int itemID)
    {
        if (!stats.ContainsKey(itemID))
        {
            stats[itemID] = new PoolItemStats { ItemID = itemID };
        }
        return stats[itemID];
    }

    public List<PoolItemStats> GetAllStats()
    {
        return new List<PoolItemStats>(stats.Values);
    }

    public PoolItemStats GetStats(int itemID)
    {
        return stats.ContainsKey(itemID) ? stats[itemID] : null;
    }
}

[System.Serializable]
public class PoolItemStats
{
    public int ItemID;
    public string ItemName;
    public int TotalCreated;
    public int TotalGets;
    public int TotalReleases;
    public int TotalDestroyed;

    public int CurrentlyActive => TotalGets - TotalReleases;
}
#endregion