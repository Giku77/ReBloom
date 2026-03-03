using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using static BansheeGz.BGDatabase.BGJsonRepoModel;

public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform itemParent;

    [Header("Object Pool Settings")]
    [SerializeField] private int defaultPoolSize = 10;
    [SerializeField] private int maxPoolSize = 200; // 디버그용 대량 생성 고려
    [SerializeField] private bool collectionCheck = true;

    [Header("Batch Spawn Settings")]
    [SerializeField] private float batchSpawnRadius = 4f; // 대량 생성 시 퍼지는 반경
    [SerializeField] private int maxSpawnPerFrame = 10;   // 프레임당 최대 생성 수

    // 아이템 ID별 오브젝트 풀
    private Dictionary<int, ObjectPool<GameObject>> itemPools = new Dictionary<int, ObjectPool<GameObject>>();

    // 프리팹 캐시
    private Dictionary<int, GameObject> prefabCache = new Dictionary<int, GameObject>();

    // 디버그용 통계
    public PoolStatistics Statistics { get; private set; } = new PoolStatistics();

    private bool IsNetworkSession =>
    NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private bool CanServerSpawnNetworkItem =>
        IsNetworkSession && NetworkManager.Singleton.IsServer;

    #region 단일 아이템 스폰

    //public async UniTask<GameObject> SpawnItemInWorld(ItemBase itemData, Vector3 position, CancellationToken ctx)
    //{
    //    if (itemData == null)
    //    {
    //        Debug.LogError($"[ItemSpawner] 아이템 데이터가 null입니다!");
    //        return null;
    //    }

    //    int itemID = itemData.itemID;

    //    // 풀이 없으면 생성 시도
    //    if (!itemPools.ContainsKey(itemID))
    //    {
    //        CreatePoolForItem(itemData);

    //        // 생성 후에도 없으면 기본 프리팹 사용
    //        if (!itemPools.ContainsKey(itemID))
    //        {
    //            Debug.LogError($"[ItemSpawner] Pool 생성 실패: {itemData.itemName}");
    //            return await SpawnDefaultItem(position);  // 기본 아이템 스폰
    //        }
    //    }

    //    // 안전하게 가져오기
    //    if (itemPools.TryGetValue(itemID, out ObjectPool<GameObject> pool))
    //    {
    //        GameObject itemObj = pool.Get();
    //        itemObj.transform.position = position;
    //        itemObj.transform.rotation = Quaternion.identity;

    //        var worldItem = itemObj.GetComponent<WorldItem>();
    //        worldItem?.Initialize(itemData);

    //        return itemObj;
    //    }

    //    return null;
    //}

    public NetworkWorldItem SpawnNetworkItemInWorld(ItemBase itemData, Vector3 position, int quantity, bool persistent)
    {
        if (!NetworkManager.Singleton.IsServer) return null;

        GameObject obj = Instantiate(itemData.itemPrefab, position, Quaternion.identity, itemParent);

        var netItem = obj.GetComponent<NetworkWorldItem>();
        var netObj = obj.GetComponent<NetworkObject>();

        netItem.InitializeServer(itemData, quantity, persistent);
        netObj.Spawn();

        return netItem;
    }

    public async UniTask PrewarmOneItemAsync(int itemID, int count, CancellationToken ct)
    {
        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null) return;

        if (!itemPools.ContainsKey(itemID))
        {
            bool ok = await CreatePoolForItemAsync(item, ct);
            if (!ok) return;
        }

        var pool = itemPools[itemID];

        // count 만큼 실제 1~2개만 생성해서 바로 되돌림(=첫 Instantiate 비용을 미리 지불)
        var temp = new GameObject[count];
        for (int i = 0; i < count; i++)
            temp[i] = pool.Get();

        for (int i = 0; i < count; i++)
            pool.Release(temp[i]);
    }

    public async UniTask<GameObject> SpawnItemInWorld(ItemBase itemData, Vector3 position, CancellationToken ctx)
    {
        if (itemData == null)
        {
            Debug.LogError("[ItemSpawner] 아이템 데이터가 null입니다!");
            return null;
        }

        // 멀티플레이 + 서버(호스트 포함)면 네트워크 스폰 경로 사용
        if (CanServerSpawnNetworkItem)
        {
            return await SpawnNetworkItemInWorld(
                itemData,
                position,
                quantity: 1,
                persistent: false,
                applyDropPhysics: false,
                ctx
            );
        }

        // 멀티플레이 클라이언트는 직접 월드 스폰하면 안 됨
        if (IsNetworkSession)
        {
            Debug.LogWarning("[ItemSpawner] 클라이언트는 직접 SpawnItemInWorld를 호출할 수 없습니다. 서버 RPC를 통해 요청해야 합니다.");
            return null;
        }

        // 싱글/로컬 경로
        return await SpawnItemInWorldLocal(itemData, position, ctx);
    }

    public async UniTask<GameObject> SpawnPersistentItemInWorld(ItemBase itemData, Vector3 position, int quantity, CancellationToken ctx)
    {
        if (itemData == null)
        {
            Debug.LogError("[ItemSpawner] 아이템 데이터가 null입니다!");
            return null;
        }

        if (CanServerSpawnNetworkItem)
        {
            return await SpawnNetworkItemInWorld(
                itemData,
                position,
                quantity,
                persistent: true,
                applyDropPhysics: false,
                ctx
            );
        }

        if (IsNetworkSession)
        {
            Debug.LogWarning("[ItemSpawner] 클라이언트는 직접 영구 월드 아이템을 생성할 수 없습니다.");
            return null;
        }

        GameObject itemObj;

        if (quantity > 1)
            itemObj = await DropItemWithQuantityLocal(itemData, position, quantity);
        else
            itemObj = await SpawnItemInWorldLocal(itemData, position, ctx);

        if (itemObj != null && itemObj.TryGetComponent<WorldItem>(out var worldItem))
        {
            worldItem.SetPersistent(true);
        }

        return itemObj;
    }


    // 기본 아이템 스폰 (폴백)
    private async UniTask<GameObject> SpawnDefaultItem(Vector3 position)
    {
        const string DEFAULT_ITEM_PATH = "Item/Item00";

        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(DEFAULT_ITEM_PATH);
            GameObject prefab = await handle.WithCancellation(this.GetCancellationTokenOnDestroy());

            GameObject itemObj = Instantiate(prefab, position, Quaternion.identity, itemParent);
            Debug.Log($"[ItemSpawner] 기본 아이템 생성됨");
            return itemObj;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemSpawner] 기본 아이템도 실패: {e.Message}");
            return null;
        }
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
    public async UniTask<List<GameObject>> SpawnItemBatch(ItemBase itemData, Vector3 centerPosition, int count, float scatterRadius = 0f)
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
            CreatePoolForItem(itemData);
        }

        // 프레임 분산 생성 (너무 많으면 여러 프레임에 나눠서)
        int spawnedThisFrame = 0;

        for (int i = 0; i < count; i++)
        {
            // 랜덤 위치 계산 (원형으로 퍼지기)
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * scatterRadius;
            randomOffset.y = 0; // Y축은 고정
            Vector3 spawnPosition = centerPosition + randomOffset;

            // 아이템 생성
            GameObject itemObj = await SpawnItemInWorld(itemData, spawnPosition, this.GetCancellationTokenOnDestroy());
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
                    rb.AddForce(pushDirection * UnityEngine.Random.Range(1f, 3f), ForceMode.Impulse);
                    rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 2f;
                }
            }

            // 프레임당 생성 수 제한
            spawnedThisFrame++;
            if (spawnedThisFrame >= maxSpawnPerFrame)
            {
                spawnedThisFrame = 0;
                await UniTask.Yield(); // 다음 프레임으로 양보
            }
        }

        Debug.Log($"[ItemSpawner] 대량 생성 완료: {itemData.itemName} x{count}개");
        return spawnedItems;
    }

    /// <summary>
    /// 스택 아이템 드롭 (수량 설정 가능)
    /// </summary>
    public async UniTask<GameObject> DropItemWithQuantity(ItemBase itemData, Vector3 position, int quantity)
    {
        if (itemData == null)
        {
            Debug.LogError("[ItemSpawner] 아이템 데이터가 null입니다!");
            return null;
        }

        if (CanServerSpawnNetworkItem)
        {
            return await SpawnNetworkItemInWorld(
                itemData,
                position,
                quantity,
                persistent: false,
                applyDropPhysics: true,
                this.GetCancellationTokenOnDestroy()
            );
        }

        if (IsNetworkSession)
        {
            Debug.LogWarning("[ItemSpawner] 멀티플레이 클라이언트는 직접 드랍 아이템을 생성할 수 없습니다. 서버 RPC가 필요합니다.");
            return null;
        }

        return await DropItemWithQuantityLocal(itemData, position, quantity);
    }

    private async UniTask<GameObject> SpawnItemInWorldLocal(ItemBase itemData, Vector3 position, CancellationToken ctx)
    {
        int itemID = itemData.itemID;

        if (!itemPools.ContainsKey(itemID))
        {
            bool success = await CreatePoolForItemAsync(itemData, ctx);

            if (!success || !itemPools.ContainsKey(itemID))
            {
                Debug.LogError($"[ItemSpawner] Pool 생성 실패: {itemData.itemName}");
                return await SpawnDefaultItem(position);
            }
        }

        if (itemPools.TryGetValue(itemID, out ObjectPool<GameObject> pool))
        {
            GameObject itemObj = pool.Get();
            itemObj.transform.position = position;
            itemObj.transform.rotation = Quaternion.identity;

            var worldItem = itemObj.GetComponent<WorldItem>();
            worldItem?.Initialize(itemData);

            return itemObj;
        }

        return null;
    }

    private async UniTask<GameObject> DropItemWithQuantityLocal(ItemBase itemData, Vector3 position, int quantity)
    {
        GameObject itemObj = await SpawnItemInWorldLocal(itemData, position, this.GetCancellationTokenOnDestroy());

        if (itemObj != null)
        {
            if (itemObj.TryGetComponent<WorldItem>(out var worldItem))
            {
                worldItem.SetQuantity(quantity);
            }

            if (itemObj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 2f;
            }
        }

        return itemObj;
    }

    private async UniTask<GameObject> SpawnNetworkItemInWorld(
    ItemBase itemData,
    Vector3 position,
    int quantity,
    bool persistent,
    bool applyDropPhysics,
    CancellationToken ctx)
    {
        if (!CanServerSpawnNetworkItem)
        {
            Debug.LogWarning("[ItemSpawner] SpawnNetworkItemInWorld는 서버에서만 호출해야 합니다.");
            return null;
        }

        GameObject prefab = await LoadItemPrefabAsync(itemData, ctx);
        if (prefab == null)
        {
            Debug.LogError($"[ItemSpawner] 네트워크 스폰용 프리팹 로드 실패: {itemData.itemName}");
            return null;
        }

        if (!prefab.TryGetComponent<NetworkObject>(out _))
        {
            Debug.LogError($"[ItemSpawner] 프리팹에 NetworkObject가 없습니다: {prefab.name}");
            return null;
        }

        if (!prefab.TryGetComponent<NetworkWorldItem>(out _))
        {
            Debug.LogError($"[ItemSpawner] 프리팹에 NetworkWorldItem이 없습니다: {prefab.name}");
            return null;
        }

        GameObject itemObj = Instantiate(prefab, position, Quaternion.identity, itemParent);
        itemObj.name = $"NetItem_{itemData.itemID}";

        var netItem = itemObj.GetComponent<NetworkWorldItem>();
        var netObj = itemObj.GetComponent<NetworkObject>();

        if (netItem == null || netObj == null)
        {
            Debug.LogError("[ItemSpawner] NetworkWorldItem 또는 NetworkObject를 찾을 수 없습니다.");
            Destroy(itemObj);
            return null;
        }

        // Spawn 전에 네트워크 변수 세팅
        netItem.InitializeServer(itemData, quantity, persistent);

        // 실제 네트워크 spawn
        netObj.Spawn();

        if (applyDropPhysics && itemObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 2f;
        }

        return itemObj;
    }
    #endregion

    #region 오브젝트 풀 관리 (비동기 추가)

    /// <summary>
    /// 비동기로 아이템 풀 생성 (프리팹 로드 후)
    /// </summary>
    private async UniTask<bool> CreatePoolForItemAsync(ItemBase itemData, CancellationToken ctx)
    {
        int itemID = itemData.itemID;

        // 이미 풀이 있으면 스킵
        if (itemPools.ContainsKey(itemID))
        {
            return true;
        }

        // 프리팹 비동기 로드
        GameObject prefab = await LoadItemPrefabAsync(itemData, ctx);
        if (prefab == null)
        {
            Debug.LogError($"[ItemSpawner] 프리팹 로드 실패로 풀 생성 불가: {itemData.itemName}");
            return false;
        }

        // 풀 생성
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

        //Debug.Log($"[ItemSpawner] 오브젝트 풀 생성 완료: {itemData.itemName} (ID: {itemID})");
        return true;
    }

    // 기존 동기 버전은 유지 (fallback용)
    private void CreatePoolForItem(ItemBase itemData)
    {
        int itemID = itemData.itemID;

        GameObject prefab = LoadOrGetCachedPrefab(itemData);
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

        //하이라이트 추가 하기 위해서 생성 될 때 컴포넌트 추가
        if (obj.GetComponent<InteractionHighlight>() == null)
        {
            obj.AddComponent<InteractionHighlight>();
        }

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
            //rb.isKinematic = true;
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
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

       if(obj.TryGetComponent<WorldItem>(out var wordItem))
        {
            wordItem.ResetItem();
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

    #region 프리팹 로딩
    private GameObject LoadOrGetCachedPrefab(ItemBase itemData)
    {
        int itemID = itemData.itemID;

        if (prefabCache.ContainsKey(itemID))
        {
            return prefabCache[itemID];
        }

        // 캐시에 없으면 동기 로드 시도 (이미 로드된 경우만)
        GameObject prefab = itemData.itemPrefab;
        if (prefab != null)
        {
            prefabCache[itemID] = prefab;
        }

        return prefab;
    }

    /// <summary>
    /// Addressable로 프리팹을 비동기 로드
    /// </summary>
    private async UniTask<GameObject> LoadItemPrefabAsync(ItemBase itemData, CancellationToken ctx)
    {
        int itemID = itemData.itemID;

        // 이미 캐시에 있으면 반환
        if (prefabCache.ContainsKey(itemID))
        {
            return prefabCache[itemID];
        }

        string addressPath = itemData.worldPrefabAddress; // ItemBase에 이 필드가 있다고 가정

        if (string.IsNullOrEmpty(addressPath))
        {
            // 경로가 없으면 직접 참조 사용
            GameObject prefab = itemData.itemPrefab;
            if (prefab != null)
            {
                Debug.LogWarning($"[ItemSpawner] Address 없음, 직접 참조 사용: id={itemID}, name={itemData.itemName}, prefab={prefab.name}");
                prefabCache[itemID] = prefab;
                return prefab;
            }

            Debug.LogError($"[ItemSpawner] 프리팹 경로와 직접 참조 둘 다 없음: id={itemID}, name={itemData.itemName}");
            return null;
        }

        //Debug.Log($"[ItemSpawner] LoadItemPrefabAsync 시작: id={itemID}, name={itemData.itemName}, address='{addressPath}'");

        AsyncOperationHandle<GameObject> handle = default;

        try
        {
            handle = Addressables.LoadAssetAsync<GameObject>(addressPath);
            GameObject prefab = await handle.WithCancellation(ctx);

            if (handle.Status != AsyncOperationStatus.Succeeded || prefab == null)
            {
                Debug.LogError(
                    $"[ItemSpawner] 프리팹 로드 실패\n" +
                    $"  id={itemID}, name={itemData.itemName}\n" +
                    $"  address='{addressPath}'\n" +
                    $"  status={handle.Status}, exception={handle.OperationException}"
                );
                return null;
            }

            //Debug.Log($"[ItemSpawner] 프리팹 로드 성공: id={itemID}, name={itemData.itemName}, prefab={prefab.name}, address='{addressPath}'");
            prefabCache[itemID] = prefab;
            return prefab;
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[ItemSpawner] 프리팹 로드 취소됨: id={itemID}, name={itemData.itemName}, address='{addressPath}'");
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[ItemSpawner] 예외로 인한 프리팹 로드 실패\n" +
                $"  id={itemID}, name={itemData.itemName}\n" +
                $"  address='{addressPath}'\n" +
                $"  exception={e}"
            );
        }

        return null;
    }
    //{
    //    int itemID = itemData.itemID;

    //    // 이미 캐시에 있으면 반환
    //    if (prefabCache.ContainsKey(itemID))
    //    {
    //        return prefabCache[itemID];
    //    }

    //    // Addressable 경로가 있는지 확인
    //    string addressPath = itemData.worldPrefabAddress; // ItemBase에 이 필드가 있다고 가정

    //    if (string.IsNullOrEmpty(addressPath))
    //    {
    //        // 경로가 없으면 직접 참조 사용
    //        GameObject prefab = itemData.itemPrefab;
    //        if (prefab != null)
    //        {
    //            prefabCache[itemID] = prefab;
    //            return prefab;
    //        }

    //        Debug.LogError($"[ItemSpawner] 프리팹 경로와 직접 참조 둘 다 없음: {itemData.itemName}");
    //        return null;
    //    }

    //    try
    //    {
    //        // Addressable로 비동기 로드
    //        var handle = Addressables.LoadAssetAsync<GameObject>(addressPath);
    //        GameObject prefab = await handle.WithCancellation(ctx);

    //        if (prefab != null)
    //        {
    //            prefabCache[itemID] = prefab;
    //            //Debug.Log($"[ItemSpawner] 프리팹 로드 완료: {itemData.itemName}");
    //            return prefab;
    //        }
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        Debug.Log($"[ItemSpawner] 프리팹 로드 취소됨: {itemData.itemName}");
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogError($"[ItemSpawner] 프리팹 로드 실패: {itemData.itemName}, {e.Message}");
    //    }

    //    return null;
    //}
    #endregion

    #region 유틸리티 (비동기)

    /// <summary>
    /// 아이템 풀을 미리 로드하고 생성 (비동기)
    /// </summary>
    public async UniTask PreloadItemPoolAsync(int itemID, int count, CancellationToken ctx)
    {
        ItemBase itemData = ItemDatabase.I.GetItem(itemID);
        if (itemData == null)
        {
            Debug.LogWarning($"[ItemSpawner] 존재하지 않는 아이템 ID: {itemID}");
            return;
        }

        // 풀이 없으면 비동기로 생성
        if (!itemPools.ContainsKey(itemID))
        {
            bool success = await CreatePoolForItemAsync(itemData, ctx);
            if (!success)
            {
                Debug.LogError($"[ItemSpawner] 풀 생성 실패: {itemData.itemName}");
                return;
            }
        }

        // 풀에서 오브젝트 미리 생성
        ObjectPool<GameObject> pool = itemPools[itemID];
        List<GameObject> preloadedObjects = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            preloadedObjects.Add(pool.Get());
        }

        // 다시 풀로 반환
        foreach (var obj in preloadedObjects)
        {
            pool.Release(obj);
        }

        //Debug.Log($"[ItemSpawner] 프리로드 완료: {itemData.itemName} (ID: {itemID}), {count}개");
    }

    /// <summary>
    /// 모든 아이템 프리로드 (씬 시작 시)
    /// </summary>
    public async UniTask PreloadAllItemsAsync(CancellationToken ctx)
    {
        var allItems = ItemDatabase.I.GetAllItems();

        Debug.Log($"[ItemSpawner] 전체 아이템 프리로드 시작: {allItems.Count}개");

        foreach (var item in allItems)
        {
            await PreloadItemPoolAsync(item.itemID, defaultPoolSize, ctx);

            // 프레임 양보 (한 번에 너무 많이 로드하면 프레임 드랍)
            await UniTask.Yield();
        }

        Debug.Log($"[ItemSpawner] 전체 아이템 프리로드 완료!");
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

    #region Unity 생명주기
    private async void Start()
    {
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile) return;
        try
        {
            await PreloadAllItemsAsync(this.GetCancellationTokenOnDestroy());
            Debug.Log("[ItemSpawner] 초기화 완료 - 게임 시작 가능");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[ItemSpawner] 프리로드 취소됨");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemSpawner] 프리로드 중 오류: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        ClearAllPools();

        // Addressable 핸들 정리
        foreach (var kvp in prefabCache)
        {
            if (kvp.Value != null)
            {
                Addressables.Release(kvp.Value);
            }
        }
    }
    #endregion

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