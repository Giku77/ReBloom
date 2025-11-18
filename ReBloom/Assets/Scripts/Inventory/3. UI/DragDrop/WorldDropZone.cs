using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 퀵슬롯 개별 슬롯에 붙이는 드롭존
/// 인벤토리에서 드래그한 아이템을 퀵슬롯에 할당
/// </summary>
public class WorldDropZone : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private InventoryItemData inventoryItemData; // 인벤토리 데이터

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
    [SerializeField] private int debugSpawnCount = 1; // 디버그용 아이템 몇 개 생성

    private bool isPointerOver = false;

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
            Debug.LogWarning("[WorldDropZone] 드래그한 아이템이 없습니다.");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[WorldDropZone] 플레이어 Transform을 찾을 수 없습니다!");
            return;
        }

        Vector3 dropPosition = CalculateDropPosition();

        if (itemSpawner != null)
        {
            try
            {
                // 디버그 인벤토리 여부 확인
                bool isFromDebugInventory = IsFromDebugInventory(eventData);

                if (isFromDebugInventory)
                {
                    // 디버그 모드: 아이템 생성
                    await HandleDebugDrop(draggedItem, dropPosition);
                }
                else
                {
                    // 게임 모드: 인벤토리에서 드롭
                    await HandleGameDrop(draggedItem, dropPosition);
                }
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

        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }
    #endregion

    #region Drop Handling
    /// <summary>
    ///  디버그 인벤토리에서 드롭한 것인지 확인
    /// </summary>
    private bool IsFromDebugInventory(PointerEventData eventData)
    {
        // 드롭된 아이템이 디버그 인벤토리에서 온 것인지 확인
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
    /// 게임 모드: 인벤토리에서 드롭
    /// </summary>
    private async UniTask HandleGameDrop(
        ItemBase draggedItem, Vector3 dropPosition)
    {
        if (inventoryItemData == null)
        {
            Debug.LogError("[WorldDropZone] InventoryItemData를 찾을 수 없습니다!");
            return;
        }

        int itemCount = inventoryItemData.GetItemCount(draggedItem.itemID);

        if (itemCount <= 0)
        {
            Debug.LogWarning($"[WorldDropZone] {draggedItem.itemName}의(가) 인벤토리에 없습니다.");
            return;
        }

        // 아이템 생성
        for (int i = 0; i < itemCount; i++)
        {
            await itemSpawner.DropItem(draggedItem, dropPosition, Vector3.zero);
        }

        // 인벤토리에서 제거
        inventoryItemData.RemoveItem(draggedItem.itemID, itemCount);

        Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{itemCount}개를 드롭했습니다.");
    }

    /// <summary>
    /// 디버그 인벤토리에서 드롭한 것인지 확인
    /// </summary>
    private async UniTask HandleDebugDrop(
        ItemBase draggedItem, Vector3 dropPosition)
    {
        // 아이템 생성
        for (int i = 0; i < debugSpawnCount; i++)
        {
            await itemSpawner.DropItem(draggedItem, dropPosition, Vector3.zero);
        }

        Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{debugSpawnCount}개를 드롭했습니다. (디버그 모드)");
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