using UnityEngine;

/// <summary>
/// 플레이어 사망 처리 및 시체박스 생성 관리
/// PlayerStats에서 자동으로 이벤트 등록됨
/// </summary>
public class DeathBoxHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryItemData playerInventory;
    [SerializeField] private DeathBoxData deathBoxData;

    [Header("Death Box Settings")]
    [SerializeField] private GameObject deathBoxPrefab;
    [SerializeField] private Vector3 dropOffset = Vector3.zero;

    [Header("Options")]
    [SerializeField] private bool autoSpawnDeathBox = true;
    [SerializeField] private bool clearInventoryOnDeath = true;

    private GameObject currentDeathBox;

    /// <summary>
    /// 플레이어 사망 처리 (PlayerStats에서 자동 호출)
    /// </summary>
    public void OnCreateDeathBox()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("[DeathBoxHandler] 플레이어를 찾을 수 없습니다!");
            return;
        }

        Transform playerTransform = player.transform;

        if (playerInventory == null || deathBoxData == null)
        {
            Debug.LogError("[DeathBoxHandler] 필수 참조가 없습니다!");
            return;
        }

        // 1. 인벤토리 아이템을 시체박스로 이동
        Vector3 deathPosition = playerTransform.position + dropOffset;
        deathBoxData.StoreItemsFromInventory(playerInventory, deathPosition);

        // 2. 인벤토리 클리어
        if (clearInventoryOnDeath)
        {
            playerInventory.Clear();
            Debug.Log("[DeathBoxHandler] 인벤토리를 비웠습니다.");
        }

        // 3. 시체박스 오브젝트 생성
        if (autoSpawnDeathBox && deathBoxPrefab != null)
        {
            SpawnDeathBox(deathPosition);
        }

        Debug.Log($"[DeathBoxHandler] 플레이어 사망 처리 완료. 위치: {deathPosition}");
    }

    private void SpawnDeathBox(Vector3 position)
    {
        if (currentDeathBox != null)
        {
            Destroy(currentDeathBox);
        }

        currentDeathBox = Instantiate(deathBoxPrefab, position, Quaternion.identity);

        var deathBoxInteract = currentDeathBox.GetComponent<WorldDeathBox>();
        if (deathBoxInteract != null)
        {
            deathBoxInteract.Initialize(deathBoxData, playerInventory);
        }

        Debug.Log($"[DeathBoxHandler] 시체박스 생성: {position}");
    }
}