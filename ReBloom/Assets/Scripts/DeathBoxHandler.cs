using UnityEngine;

/// <summary>
/// 플레이어 사망 처리 및 시체박스 생성 관리
/// </summary>
public class DeathBoxHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryItemData playerInventory;
    [SerializeField] private DeathBoxData deathBoxData;

    [Header("Death Box Settings")]
    [SerializeField] private GameObject deathBoxPrefab; // 시체박스 프리팹
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 dropOffset = Vector3.zero; // 드롭 위치 오프셋

    [Header("Options")]
    [SerializeField] private bool autoSpawnDeathBox = true; // 자동 생성 여부
    [SerializeField] private bool clearInventoryOnDeath = true; // 인벤토리 자동 클리어

    private GameObject currentDeathBox; // 현재 생성된 시체박스

    private void Awake()
    {
        playerTransform = GameObject.FindWithTag("Player").transform;

        if (deathBoxData == null)
        {
            Debug.LogError("[PlayerDeathHandler] DeathBoxData를 할당해주세요!");
        }
    }

    /// <summary>
    /// 플레이어 사망 처리 (외부에서 호출)
    /// </summary>
    public void OnCreateDeathBox()
    {
        if (playerInventory == null || deathBoxData == null)
        {
            Debug.LogError("[PlayerDeathHandler] 필수 참조가 없습니다!");
            return;
        }

        // 1. 인벤토리 아이템을 시체박스로 이동
        Vector3 deathPosition = playerTransform.position + dropOffset;
        deathBoxData.StoreItemsFromInventory(playerInventory, deathPosition);

        // 2. 인벤토리 클리어
        if (clearInventoryOnDeath)
        {
            playerInventory.Clear();
            Debug.Log("[PlayerDeathHandler] 인벤토리를 비웠습니다.");
        }

        // 3. 시체박스 오브젝트 생성
        if (autoSpawnDeathBox && deathBoxPrefab != null)
        {
            SpawnDeathBox(deathPosition);
        }

        Debug.Log($"[PlayerDeathHandler] 플레이어 사망 처리 완료. 위치: {deathPosition}");
    }

    /// <summary>
    /// 시체박스 오브젝트 생성
    /// </summary>
    private void SpawnDeathBox(Vector3 position)
    {
        // 기존 시체박스가 있으면 제거 (선택사항)
        if (currentDeathBox != null)
        {
            Destroy(currentDeathBox);
        }

        // 새 시체박스 생성
        currentDeathBox = Instantiate(deathBoxPrefab, position, Quaternion.identity);

        // 시체박스에 데이터 연결
        var deathBoxInteract = currentDeathBox.GetComponent<WorldDeathBox>();
        if (deathBoxInteract != null)
        {
            deathBoxInteract.Initialize(deathBoxData, playerInventory);
        }

        Debug.Log($"[PlayerDeathHandler] 시체박스 생성: {position}");
    }

    /// <summary>
    /// 수동으로 시체박스 생성 (필요 시)
    /// </summary>
    public void ManualSpawnDeathBox()
    {
        if (deathBoxData.HasItems)
        {
            SpawnDeathBox(deathBoxData.DeathPosition);
        }
        else
        {
            Debug.LogWarning("[PlayerDeathHandler] 시체박스에 아이템이 없습니다!");
        }
    }

    public void Start()
    {
        Invoke(nameof(TestPlayerDeath), 5f);
    }

    #region Debug
    public void TestPlayerDeath()
    {
        OnCreateDeathBox();
    }
    #endregion
}