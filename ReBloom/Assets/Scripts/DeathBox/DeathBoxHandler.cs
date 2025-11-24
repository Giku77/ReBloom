using System.Collections.Generic;
using UnityEngine;

public class DeathBoxHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryItemData playerInventory;
    [SerializeField] private DeathBoxData deathBoxDataTemplate;

    [Header("Dog Reference")]
    [SerializeField] private DogFollower dog; // DogFollower 직접 참조

    [Header("Death Box Settings")]
    [SerializeField] private GameObject deathBoxPrefab;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Options")]
    [SerializeField] private bool autoSpawnDeathBox = true;
    [SerializeField] private bool clearInventoryOnDeath = true;

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
        }

        // 강아지
        if (dog == null)
        {
            dog = Object.FindFirstObjectByType<DogFollower>();
        }
    }

    public void OnCreateDeathBox()
    {
        if (playerTransform == null || playerInventory == null || deathBoxDataTemplate == null)
        {
            Debug.LogError("[DeathBoxHandler] 필수 레퍼런스가 없습니다!");
            return;
        }

        // 1. 스폰 위치 계산
        Vector3 spawnPosition = CalculateSpawnPosition();

        // 2. 새 DeathBoxData 인스턴스 생성
        DeathBoxData newDeathBoxData = Instantiate(deathBoxDataTemplate);
        newDeathBoxData.CreateFromInventory(playerInventory, spawnPosition);

        // 3. 인벤토리 클리어
        if (clearInventoryOnDeath)
        {
            playerInventory.Clear();
        }

        // 4. 시체박스 스폰
        if (autoSpawnDeathBox && deathBoxPrefab != null)
        {
            SpawnDeathBox(spawnPosition, newDeathBoxData);
        }

        Debug.Log($"[DeathBoxHandler] 시체박스 생성. 위치: {spawnPosition}, 총: {activeDeathBoxes.Count}개");
    }

    /// <summary>
    /// 스폰 위치 계산 - 강아지 위치 기반
    /// </summary>
    private Vector3 CalculateSpawnPosition()
    {
        // 강아지가 없으면 플레이어 위치
        if (dog == null)
        {
            Debug.LogWarning("[DeathBoxHandler] 강아지가 없어 플레이어 위치에 스폰");
            return playerTransform.position + dropOffset;
        }

        // 강아지가 가까우면 강아지 위치
        if (dog.IsNearPlayer)
        {
            Debug.Log($"[DeathBoxHandler] 강아지 위치에 스폰 (거리: {dog.DistanceToPlayer:F1}m)");
            return dog.transform.position + dropOffset;
        }

        // 강아지가 멀면: 텔포 위치 계산 -> 강아지 텔포 -> 해당 위치에 스폰
        Vector3 teleportPosition = dog.GetTeleportPosition();

        // 강아지 텔포 시도
        dog.TeleportTo(teleportPosition);

        Debug.Log($"[DeathBoxHandler] 강아지 텔포 후 스폰 (거리: {dog.DistanceToPlayer:F1}m)");
        return teleportPosition + dropOffset;
    }

    private void SpawnDeathBox(Vector3 position, DeathBoxData deathBoxData)
    {
        GameObject newDeathBox = Instantiate(deathBoxPrefab, position, Quaternion.identity);

        var deathBoxInteract = newDeathBox.GetComponent<WorldDeathBox>();
        if (deathBoxInteract != null)
        {
            deathBoxInteract.Initialize(deathBoxData, playerInventory);
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