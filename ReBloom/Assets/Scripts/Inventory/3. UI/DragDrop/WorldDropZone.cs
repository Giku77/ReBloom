using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ���� ���� �����
/// �巡�� �ҽ�(����/�����)�� ���� �ٸ��� ó��
/// </summary>
public class WorldDropZone : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private InventoryItemData inventoryItemData; // ���� �κ��丮��

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
    [SerializeField] private int debugSpawnCount = 1; // ����� ��� ���� ����

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
                Debug.LogError("[WorldDropZone] �÷��̾ ã�� �� �����ϴ�!");
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
            Debug.LogWarning("[WorldDropZone] ��ӵ� �������� �����ϴ�.");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[WorldDropZone] �÷��̾� Transform�� �����ϴ�!");
            return;
        }

        Vector3 dropPosition = CalculateDropPosition();

        if (itemSpawner != null)
        {
            try
            {
                // �巡�� �ҽ� Ȯ��
                bool isFromDebugInventory = IsFromDebugInventory(eventData);

                if (isFromDebugInventory)
                {
                    // ����� ���: ������ ����
                    await HandleDebugDrop(draggedItem, dropPosition);
                }
                else
                {
                    // ���� ���: �κ��丮���� ����
                    await HandleGameDrop(draggedItem, dropPosition);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDropZone] ������ ��� �� ����: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("[WorldDropZone] ItemSpawner�� ã�� �� �����ϴ�!");
        }

        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }
    #endregion

    #region Drop Handling
    /// <summary>
    /// ����� �κ��丮���� �巡���ߴ��� Ȯ��
    /// </summary>
    private bool IsFromDebugInventory(PointerEventData eventData)
    {
        // �巡�� ������ ������Ʈ ������Ʈ�� �Ǵ�
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
    /// ���� �κ��丮 ��� ó�� (���� ����)
    /// </summary>
    private async UniTask HandleGameDrop(
        ItemBase draggedItem, Vector3 dropPosition)
    {
        if (inventoryItemData == null)
        {
            Debug.LogError("[WorldDropZone] InventoryItemData�� �����ϴ�!");
            return;
        }

        int itemCount = inventoryItemData.GetItemCount(draggedItem.itemID);

        if (itemCount <= 0)
        {
            Debug.LogWarning($"[WorldDropZone] {draggedItem.itemName}��(��) �κ��丮�� �����ϴ�.");
            return;
        }

        // ������ ����
        for (int i = 0; i < itemCount; i++)
        {
            await itemSpawner.DropItem(draggedItem, dropPosition, Vector3.zero);
        }

        // �κ��丮���� ����
        inventoryItemData.RemoveItem(draggedItem.itemID, itemCount);

        Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{itemCount}��(��) ����߽��ϴ�.");
    }

    /// <summary>
    /// ����� �κ��丮 ��� ó�� (���� ���� ����)
    /// </summary>
    private async UniTask HandleDebugDrop(
        ItemBase draggedItem, Vector3 dropPosition)
    {
        // ������ ����
        for (int i = 0; i < debugSpawnCount; i++)
        {
            await itemSpawner.DropItem(draggedItem, dropPosition, Vector3.zero);
        }

        Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{debugSpawnCount}��(��) �����߽��ϴ�. (�����)");
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