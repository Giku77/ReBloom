using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 월드 맵에서 아이템을 드롭할 수 있는 영역
/// 플레이어 앞쪽 기준으로 아이템을 떨어뜨림
/// </summary>
public class WorldDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Transform playerTransform; // 플레이어 Transform
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 2f; // 플레이어 앞쪽 거리
    [SerializeField] private float dropHeight = 1.5f; // 떨어지는 시작 높이
    [SerializeField] private Vector3 dropOffset = Vector3.zero; // 추가 오프셋 (필요시)

    [Header("Visual Feedback")]
    [SerializeField] private GameObject dropIndicator; // 드롭 가능 영역 표시

    [Header("Ground Detection (Optional)")]
    [SerializeField] private bool useGroundDetection = true; // 지면 감지 사용 여부
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 10f;

    private bool isPointerOver = false;

    private void Awake()
    {
        // 플레이어 자동 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("[WorldDropZone] 플레이어를 찾을 수 없습니다! Player 태그를 확인하세요.");
            }
        }

        // ItemSpawner 자동 찾기
        if (itemSpawner == null)
        {
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
        }

        // 드롭 인디케이터 초기 숨김
        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        // 드래그 중이고 포인터가 이 영역 위에 있으면 미리보기 표시
        if (isPointerOver && ItemIconDragHandler.CurrentDraggedItem != null)
        {
            UpdateDropIndicator();
        }
    }

    #region Event Handlers
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        if (dropIndicator != null && ItemIconDragHandler.CurrentDraggedItem != null)
        {
            dropIndicator.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }

    public async void OnDrop(PointerEventData eventData)
    {
        ItemBase draggedItem = ItemIconDragHandler.CurrentDraggedItem;

        if (draggedItem == null)
        {
            Debug.LogWarning("[WorldDropZone] 드롭된 아이템이 없습니다.");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[WorldDropZone] 플레이어 Transform이 없습니다!");
            return;
        }

        // 플레이어 기준 드롭 위치 계산
        Vector3 dropPosition = CalculateDropPosition();

        // ItemSpawner를 통해 아이템 생성
        if (itemSpawner != null)
        {
            try
            {
                await itemSpawner.DropItem(draggedItem, dropPosition, Vector3.zero);
                Debug.Log($"[WorldDropZone] {draggedItem.itemName}을(를) {dropPosition}에 배치했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDropZone] 아이템 드롭 중 오류 발생: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("[WorldDropZone] ItemSpawner를 찾을 수 없습니다!");
        }

        // 인디케이터 숨김
        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }
    #endregion

    #region Drop Position Calculation
    /// <summary>
    /// 플레이어 앞쪽 기준으로 드롭 위치 계산
    /// </summary>
    private Vector3 CalculateDropPosition()
    {
        // 플레이어 앞쪽 방향 (Y축 고려하지 않음)
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        // 기본 드롭 위치: 플레이어 앞 + 높이
        Vector3 dropPosition = playerTransform.position
            + forward * dropDistance
            + Vector3.up * dropHeight
            + dropOffset;

        // 지면 감지가 활성화되어 있으면 지면 위치로 조정
        if (useGroundDetection)
        {
            Vector3 groundPosition = FindGroundPosition(dropPosition);
            if (groundPosition != Vector3.zero)
            {
                // 지면 위 약간 높이에서 떨어뜨림
                dropPosition = groundPosition + Vector3.up * dropHeight;
            }
        }

        return dropPosition;
    }

    /// <summary>
    /// 주어진 위치 아래의 지면 찾기
    /// </summary>
    private Vector3 FindGroundPosition(Vector3 startPosition)
    {
        // 아래쪽으로 레이캐스트
        Ray ray = new Ray(startPosition, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundRaycastDistance, groundLayer))
        {
            return hit.point;
        }

        // 지면을 찾지 못하면 원래 높이 유지
        return Vector3.zero;
    }

    /// <summary>
    /// 드롭 위치 미리보기 인디케이터 업데이트
    /// </summary>
    private void UpdateDropIndicator()
    {
        if (dropIndicator == null) return;

        Vector3 dropPosition = CalculateDropPosition();

        // 지면 위치 찾기 (인디케이터는 지면에 표시)
        if (useGroundDetection)
        {
            Vector3 groundPosition = FindGroundPosition(dropPosition);
            if (groundPosition != Vector3.zero)
            {
                dropIndicator.transform.position = groundPosition + Vector3.up * 0.1f; // 약간 띄움
                return;
            }
        }

        // 지면을 찾지 못하면 계산된 위치에서 아래쪽 평면 사용
        dropIndicator.transform.position = new Vector3(dropPosition.x, 0f, dropPosition.z);
    }
    #endregion

    #region Debug Gizmos
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // 플레이어 앞쪽 방향
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        // 드롭 위치
        Vector3 dropPos = playerTransform.position
            + forward * dropDistance
            + Vector3.up * dropHeight
            + dropOffset;

        // 드롭 위치 표시 (노란색 구)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(dropPos, 0.3f);

        // 플레이어에서 드롭 위치까지 선
        Gizmos.color = Color.green;
        Gizmos.DrawLine(playerTransform.position, dropPos);

        // 지면 감지 레이캐스트 표시
        if (useGroundDetection)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(dropPos, dropPos + Vector3.down * groundRaycastDistance);
        }
    }
    #endregion
}