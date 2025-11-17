using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 통합 월드 드롭존
/// 드래그 소스(게임/디버그)에 따라 다르게 처리
/// </summary>
public class WorldDropZone : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private InventoryItemData inventoryItemData; // 게임 인벤토리용

    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 2f;
    [SerializeField] private float dropHeight = 1.5f;
    [SerializeField] private Vector3 dropOffset = Vector3.zero;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject dropIndicator;

    [Header("Ground Detection")]
    [SerializeField] private bool useGroundDetection = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 10f;

    [Header("Debug Settings")]
    [SerializeField] private int debugSpawnCount = 1; // 디버그 모드 생성 개수

    private bool isPointerOver = false;
    private QuestUI questUI;

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("[WorldDropZone] 플레이어를 찾을 수 없습니다!");
            }
        }

        if (itemSpawner == null)
        {
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
        }

        if (inventoryItemData == null)
        {
            inventoryItemData = FindFirstObjectByType<InventoryItemData>();
        }

        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }

        questUI = FindFirstObjectByType<QuestUI>();
    }

    private void Update()
    {
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

        Vector3 dropPosition = CalculateDropPosition();

        if (itemSpawner != null)
        {
            try
            {
                // 드래그 소스 확인
                bool isFromDebugInventory = IsFromDebugInventory(eventData);

                if (isFromDebugInventory)
                {
                    // 디버그 모드: 무제한 생성
                    await HandleDebugDrop(draggedItem, dropPosition);
                }
                else
                {
                    // 게임 모드: 인벤토리에서 제거
                    await HandleGameDrop(draggedItem, dropPosition);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDropZone] 아이템 드롭 중 오류: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("[WorldDropZone] ItemSpawner를 찾을 수 없습니다!");
        }

        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }
    #endregion

    #region Drop Handling
    /// <summary>
    /// 디버그 인벤토리에서 드래그했는지 확인
    /// </summary>
    private bool IsFromDebugInventory(PointerEventData eventData)
    {
        // 드래그 시작한 오브젝트 컴포넌트로 판단
        if (eventData.pointerDrag != null)
        {
            Transform current = eventData.pointerDrag.transform;
            while (current != null)
            {
                if (current.GetComponent<DebugInventoryMarker>() != null)
                {
                    return true;
                }
                current = current.parent;
            }
        }

        return false;
    }

    /// <summary>
    /// 게임 인벤토리 드롭 처리 (수량 차감)
    /// </summary>
    private async System.Threading.Tasks.Task HandleGameDrop(
        ItemBase draggedItem, Vector3 dropPosition)
    {
        if (inventoryItemData == null)
        {
            Debug.LogError("[WorldDropZone] InventoryItemData가 없습니다!");
            return;
        }

        int itemCount = inventoryItemData.GetItemCount(draggedItem.itemID);

        if (itemCount <= 0)
        {
            Debug.LogWarning($"[WorldDropZone] {draggedItem.itemName}이(가) 인벤토리에 없습니다.");
            return;
        }

        // 아이템 생성
        for (int i = 0; i < itemCount; i++)
        {
            await itemSpawner.DropItem(draggedItem, dropPosition, Vector3.zero);
        }

        // 인벤토리에서 제거
        inventoryItemData.RemoveItem(draggedItem.itemID, itemCount);
        questUI?.Refresh();

        Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{itemCount}을(를) 드롭했습니다.");
    }

    /// <summary>
    /// 디버그 인벤토리 드롭 처리 (수량 차감 없음)
    /// </summary>
    private async System.Threading.Tasks.Task HandleDebugDrop(
        ItemBase draggedItem, Vector3 dropPosition)
    {
        // 무제한 생성
        for (int i = 0; i < debugSpawnCount; i++)
        {
            await itemSpawner.DropItem(draggedItem, dropPosition, Vector3.zero);
        }

        Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{debugSpawnCount}을(를) 생성했습니다. (디버그)");
    }
    #endregion

    #region Drop Position Calculation
    private Vector3 CalculateDropPosition()
    {
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 dropPosition = playerTransform.position
            + forward * dropDistance
            + Vector3.up * dropHeight
            + dropOffset;

        if (useGroundDetection)
        {
            Vector3 groundPosition = FindGroundPosition(dropPosition);
            if (groundPosition != Vector3.zero)
            {
                dropPosition = groundPosition + Vector3.up * dropHeight;
            }
        }

        return dropPosition;
    }

    private Vector3 FindGroundPosition(Vector3 startPosition)
    {
        Ray ray = new Ray(startPosition, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundRaycastDistance, groundLayer))
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    private void UpdateDropIndicator()
    {
        if (dropIndicator == null) return;

        Vector3 dropPosition = CalculateDropPosition();

        if (useGroundDetection)
        {
            Vector3 groundPosition = FindGroundPosition(dropPosition);
            if (groundPosition != Vector3.zero)
            {
                dropIndicator.transform.position = groundPosition + Vector3.up * 0.1f;
                return;
            }
        }

        dropIndicator.transform.position = new Vector3(dropPosition.x, 0f, dropPosition.z);
    }
    #endregion
}