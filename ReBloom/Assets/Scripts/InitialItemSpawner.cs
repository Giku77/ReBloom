using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 시작 시 월드에 아이템을 미리 배치하는 스크립트
/// </summary>
public class InitialItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnItemInfo
    {
        [Tooltip("스폰할 아이템 ID")]
        public int itemID;
        
        [Tooltip("스폰 위치 Transform")]
        public Transform spawnTransform;
        
        [Tooltip("스폰 개수 (스택 아이템용)")]
        [Range(1, 999)]
        public int quantity = 1;

        public Vector3 Position => spawnTransform != null ? spawnTransform.position : Vector3.zero;
    }

    [Header("스폰 설정")]
    [SerializeField] private SpawnItemInfo[] itemsToSpawn;

    [Header("랜덤 스폰 설정 (선택)")]
    [SerializeField] private bool useRandomSpawn = false;
    [SerializeField] private Transform[] randomSpawnPoints;
    [SerializeField] private int[] randomItemIDs;
    [SerializeField] private int randomSpawnCount = 5;

    [Header("스폰 딜레이")]
    [SerializeField] private float spawnDelay = 0.5f;

    [Header("참조")]
    [SerializeField] private ItemSpawner itemSpawner;

    public int[] GetPlannedItemIDs()
    {
        var set = new HashSet<int>();

        if (itemsToSpawn != null)
            foreach (var s in itemsToSpawn)
                if (s != null && s.itemID != 0) set.Add(s.itemID);

        if (useRandomSpawn && randomItemIDs != null)
            foreach (var id in randomItemIDs)
                if (id != 0) set.Add(id);

        return set.ToArray();
    }

    public async UniTask Begin()
    {
        if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
        await UniTask.WaitUntil(() => ItemDatabase.I.IsInitialized);
        await UniTask.Delay(TimeSpan.FromSeconds(spawnDelay));
        await SpawnInitialItems();
    }

    //private async void Start()
    //{
    //    if (itemSpawner == null)
    //    {
    //        itemSpawner = FindFirstObjectByType<ItemSpawner>();
    //    }

    //    if (itemSpawner == null)
    //    {
    //        Debug.LogError("[InitialItemSpawner] ItemSpawner를 찾을 수 없습니다!");
    //        return;
    //    }

    //    await UniTask.WaitUntil(() => ItemDatabase.I.IsInitialized);
    //    await UniTask.Delay(System.TimeSpan.FromSeconds(spawnDelay));
    //    await SpawnInitialItems();
    //}

    private async UniTask SpawnInitialItems()
    {
        if (useRandomSpawn)
        {
            await SpawnRandomItems();
        }
        else
        {
            await SpawnFixedItems();
        }

        Debug.Log("[InitialItemSpawner] 초기 아이템 스폰 완료!");
    }

    private async UniTask SpawnFixedItems()
    {
        if (itemsToSpawn == null || itemsToSpawn.Length == 0)
        {
            Debug.LogWarning("[InitialItemSpawner] 스폰할 아이템이 없습니다!");
            return;
        }

        foreach (var spawnInfo in itemsToSpawn)
        {
            if (spawnInfo.spawnTransform == null)
            {
                Debug.LogWarning($"[InitialItemSpawner] 아이템 ID {spawnInfo.itemID}의 스폰 Transform이 없습니다!");
                continue;
            }

            ItemBase itemData = ItemDatabase.I.GetItem(spawnInfo.itemID);
            
            if (itemData == null)
            {
                Debug.LogWarning($"[InitialItemSpawner] 아이템 ID {spawnInfo.itemID}를 찾을 수 없습니다!");
                continue;
            }

            Debug.Log(
           $"[InitialItemSpawner] 스폰 시도 - " +
           $"itemID={spawnInfo.itemID}, " +
           $"dbID={itemData.itemID}, " +
           $"name={itemData.itemName}, " +
           $"address='{itemData.worldPrefabAddress}', " +
           $"prefab={(itemData.itemPrefab != null ? itemData.itemPrefab.name : "null")}"
       );

            Vector3 spawnPosition = spawnInfo.Position;

            GameObject spawnedItem = null;
            
            if (spawnInfo.quantity > 1)
            {
                spawnedItem = await itemSpawner.DropItemWithQuantity(itemData, spawnPosition, spawnInfo.quantity);
            }
            else
            {
                spawnedItem = await itemSpawner.SpawnItemInWorld(itemData, spawnPosition, this.GetCancellationTokenOnDestroy());
            }

            // 영구 아이템으로 설정 (시간 지나도 안 사라짐)
            if (spawnedItem != null)
            {
                var worldItem = spawnedItem.GetComponent<WorldItem>();
                if (worldItem != null)
                {
                    worldItem.SetPersistent(true);
                }
            }

            Debug.Log($"[InitialItemSpawner] {itemData.itemName} x{spawnInfo.quantity} 스폰 완료 at {spawnPosition}");
            await UniTask.Yield();
        }
    }

    private async UniTask SpawnRandomItems()
    {
        if (randomSpawnPoints == null || randomSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[InitialItemSpawner] 랜덤 스폰 포인트가 없습니다!");
            return;
        }

        if (randomItemIDs == null || randomItemIDs.Length == 0)
        {
            Debug.LogWarning("[InitialItemSpawner] 랜덤 스폰 아이템 ID가 없습니다!");
            return;
        }

        for (int i = 0; i < randomSpawnCount; i++)
        {
            Transform spawnPoint = randomSpawnPoints[UnityEngine.Random.Range(0, randomSpawnPoints.Length)];
            int randomItemID = randomItemIDs[UnityEngine.Random.Range(0, randomItemIDs.Length)];
            ItemBase itemData = ItemDatabase.I.GetItem(randomItemID);

            if (itemData == null)
            {
                Debug.LogWarning($"[InitialItemSpawner] 아이템 ID {randomItemID}를 찾을 수 없습니다!");
                continue;
            }

            Vector3 spawnPosition = spawnPoint.position + UnityEngine.Random.insideUnitSphere * 2f;
            spawnPosition.y = spawnPoint.position.y;

            await itemSpawner.SpawnItemInWorld(itemData, spawnPosition, this.GetCancellationTokenOnDestroy());
            Debug.Log($"[InitialItemSpawner] {itemData.itemName} 랜덤 스폰 완료");
            await UniTask.Yield();
        }
    }
}
