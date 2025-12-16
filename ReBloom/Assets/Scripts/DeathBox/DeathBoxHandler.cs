using System.Collections.Generic;
using UnityEngine;

public class DeathBoxHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameInventory playerInventory;
    [SerializeField] private PlayerEquipManager playerEquipManager;

    [Header("POPPI Reference")]
    [SerializeField] private InventoryRobotPet poppi; // DogFollower 직접 참조

    [Header("Death Box Settings")]
    [SerializeField] private GameObject deathBoxPrefab;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Options")]
    [SerializeField] private bool autoSpawnDeathBox = true;
    [SerializeField] private bool clearInventoryOnDeath = true;
    [SerializeField] private bool unequipAllOnDeath = true;

    private List<GameObject> activeDeathBoxes = new List<GameObject>();
    private Transform playerTransform;

    private void Awake()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        // 플레이어
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;

            if (playerEquipManager == null)
            {
                playerEquipManager = player.GetComponent<PlayerEquipManager>();
            }
        }

        // 강아지
        if (poppi == null)
        {
            poppi = Object.FindFirstObjectByType<InventoryRobotPet>();
        }
    }

    public void OnCreateDeathBox()
    {
        Vector3 spawnPosition = CalculateSpawnPosition();
        DeathBoxData newDeathBoxData = ScriptableObject.CreateInstance<DeathBoxData>();

        // 1. 메타데이터 설정
        newDeathBoxData.SetMetadata(spawnPosition);

        // 2. 장착 아이템 추가
        if (playerEquipManager != null)
        {
            AddEquippedItemsToDeathBox(newDeathBoxData);
        }

        // 3. 인벤토리 아이템 추가
        newDeathBoxData.AddItemsFromInventory(playerInventory.Container);

        // 4. 원본 클리어
        if (clearInventoryOnDeath)
        {
            playerInventory.Clear();
        }

        // 5. 시체박스 스폰
        if (autoSpawnDeathBox && deathBoxPrefab != null)
        {
            SpawnDeathBox(spawnPosition, newDeathBoxData);
        }

        Debug.Log($"[DeathBoxHandler] 완료");
    }
    /// <summary>
    /// 장착 아이템을 시체박스에 직접 추가 (인벤토리 우회)
    /// </summary>
    private void AddEquippedItemsToDeathBox(DeathBoxData deathBoxData)
    {
        var equippedItems = playerEquipManager.GetEquippedItems();
        int totalAdded = 0;

        foreach (var itemId in equippedItems)
        {
            if (itemId > 0)
            {
                int addedCount = deathBoxData.TryAddItem(itemId, 1);

                if (addedCount > 0)
                {
                    totalAdded++;
                    Debug.Log($"[DeathBoxHandler] 장착 아이템 추가 성공: {itemId}");
                }
                else
                {
                    Debug.LogWarning($"[DeathBoxHandler] 시체박스 슬롯 부족! 아이템 추가 실패: {itemId}");
                }
            }
        }

        // 장착 정보 초기화
        playerEquipManager.ClearAllEquipData();

        Debug.Log($"[DeathBoxHandler] 장착 아이템 {totalAdded}/{equippedItems.Count}개 시체박스에 추가 완료");
    }
    /// <summary>
    /// 스폰 위치 계산 - 강아지 위치 기반
    /// </summary>
    private Vector3 CalculateSpawnPosition()
    {
        // 강아지가 없으면 플레이어 위치
        if (poppi == null)
        {
            Debug.LogWarning("[DeathBoxHandler] 강아지가 없어 플레이어 위치에 스폰");
            return playerTransform.position + dropOffset;
        }

        // 강아지가 가까우면 강아지 위치
        if (poppi.IsNearPlayer)
        {
            //Debug.Log($"[DeathBoxHandler] 강아지 위치에 스폰 (거리: {poppi.DistanceToPlayer:F1}m)");
            return poppi.transform.position + dropOffset;
        }

        //// 강아지가 멀면: 텔포 위치 계산 -> 강아지 텔포 -> 해당 위치에 스폰
        //Vector3 teleportPosition = poppi.GetTeleportPosition();

        // 강아지 텔포 시도
        poppi.TelePortTo();

       // Debug.Log($"[DeathBoxHandler] 강아지 텔포 후 스폰 (거리: {poppi.DistanceToPlayer:F1}m)");
        return poppi.transform.position + dropOffset;
    }

    private void SpawnDeathBox(Vector3 position, DeathBoxData deathBoxData)
    {
        GameObject newDeathBox = Instantiate(deathBoxPrefab, position, Quaternion.identity);

        var deathBoxInteract = newDeathBox.GetComponent<WorldDeathBox>();
        if (deathBoxInteract != null)
        {
            deathBoxInteract.Initialize(deathBoxData, playerInventory.Container);
        }

        activeDeathBoxes.Add(newDeathBox);

        // 제거 시 리스트 정리
        var cleanup = newDeathBox.AddComponent<DeathBoxCleanup>();
        cleanup.Initialize(this);
    }

    public void OnDeathBoxDestroyed(GameObject deathBox)
    {
        activeDeathBoxes.Remove(deathBox);
        Debug.Log($"[DeathBoxHandler] 시체박스 제거됨. 남은: {activeDeathBoxes.Count}개");
    }

    [ContextMenu("모든 시체박스 제거")]
    public void ClearAllDeathBoxes()
    {
        foreach (var box in activeDeathBoxes)
        {
            if (box != null) Destroy(box);
        }
        activeDeathBoxes.Clear();
    }

    public int ActiveDeathBoxCount => activeDeathBoxes.Count;
}

public class DeathBoxCleanup : MonoBehaviour
{
    private DeathBoxHandler handler;
    public void Initialize(DeathBoxHandler h) => handler = h;
    private void OnDestroy() => handler?.OnDeathBoxDestroyed(gameObject);
}