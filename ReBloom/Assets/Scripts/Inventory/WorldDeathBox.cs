using UnityEditor;
using UnityEngine;

/// <summary>
/// 월드에 배치된 시체박스와의 상호작용
/// 플레이어가 E키를 눌러 아이템 회수
/// </summary>
public class WorldDeathBox : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private DeathBoxData deathBoxDataRef;
    private DeathBoxData deathBoxData;
    [SerializeField] private InventoryItemData playerInventory;

    private Transform playerTransform;
    private InteractionHighlight highlight;
    public float HoldTime => 1f;

    private void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();

        // 템플릿을 복사해서 런타임 인스턴스 생성
        if (deathBoxDataRef != null)
        {
            deathBoxData = Instantiate(deathBoxDataRef);
        }
    }

    /// <summary>
    /// 외부에서 초기화 (PlayerDeathHandler에서 호출)
    /// </summary>
    public void Initialize(DeathBoxData data, InventoryItemData inventory)
    {
        if (deathBoxData == null && data != null) // 퀘스트용 임시 상자를 위한 검사
        {
            deathBoxData = Instantiate(data);
        }
        if (playerInventory == null) playerInventory = inventory;

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    private void Update()
    {
        if (playerTransform == null || deathBoxData == null) return;

        // 플레이어와의 거리 계산
        float distance = Vector3.Distance(transform.position, playerTransform.position);
    }
    private void OnDestroy()
    {
        // 런타임 인스턴스 정리
        if (deathBoxData != null && deathBoxData != deathBoxDataRef)
        {
            Destroy(deathBoxData);
        }
    }
    /// <summary>
    /// 모든 아이템 회수
    /// </summary>
    private void RetrieveAllItems()
    {
        if (deathBoxData == null || playerInventory == null)
        {
            Debug.LogError("[DeathBoxInteraction] 데이터가 초기화되지 않았습니다!");
            return;
        }

        if (!deathBoxData.HasItems)
        {
            Debug.LogWarning("[DeathBoxInteraction] 시체박스가 비어있습니다!");
            return;
        }

        // 아이템 회수
        deathBoxData.RetrieveItemsToInventory(playerInventory);

        Debug.Log("[DeathBoxInteraction] 모든 아이템을 회수했습니다!");

        if (highlight != null)
        {
            highlight.Hide();
        }
        // 시체박스 제거
        Destroy(gameObject);
    }

    public void Interact(PlayerController player)
    {
        Debug.Log("[WorldDeathBox] Interact");
        RetrieveAllItems();
    }
}