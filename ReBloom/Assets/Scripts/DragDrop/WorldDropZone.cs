using Cysharp.Threading.Tasks;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 퀵슬롯 개별 슬롯에 붙이는 드롭존
/// 인벤토리에서 드래그한 아이템을 퀵슬롯에 할당
/// </summary>
public class WorldDropZone : MonoBehaviour,
 /*   IDropHandler,*/ IPointerEnterHandler, IPointerExitHandler, IDropTarget
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private InventoryItemData inventoryItemData; // 인벤토리 데이터
    [SerializeField] private RemovePopUp removePopUp; // 삭제 팝업 컨트롤러

    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 2f;
    [SerializeField] private float dropHeight = 1.5f;
    [SerializeField] private Vector3 dropOffset = Vector3.zero;

    [Header("Ground Detection")]
    [SerializeField] private bool useGroundDetection = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 10f;

    [Header("Debug Settings")]
    [SerializeField] private int debugSpawnCount = 1; // 디버그용 아이템 몇 개 생성

    private ItemBase lastDroppedItem;

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
    }

    #region Event Handlers
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
    }

    /// <summary>
    /// 이 드롭존이 받을 수 있는지 판단 - 조건 로직 집중!
    /// </summary>
    public bool CanAcceptDrop(DragContext context)
    {
        if (context?.Item == null) return false;

        // 퀵슬롯에서 월드로는 드롭 불가 -> 월드 드롭 시 퀵슬롯에서 빠짐
        //if (context.IsFromQuickSlot) return false;

        // 디버그/인벤토리에서는 가능
        return true;
    }

    /// <summary>
    /// 실제 드롭 처리
    /// </summary>
    public async void HandleDrop(DragContext context)
    {
        if (context.IsFromDebug)
        {
            // 디버그: 즉시 드롭
            Vector3 dropPosition = CalculateDropPosition();
            await HandleDebugDrop(context.Item, dropPosition);
        }
        else if (context.IsFromQuickSlot)
        {
            //// 퀵슬롯: 팝업 열고 드롭 (인벤토리 참조 제거 + 퀵슬롯 갱신)
            //removePopUp?.OnOpen(context.Item);

            // 퀵슬롯일때는 아무것도 안함.
            HandleQuickSlotRemoval(context);
        }
        else
        {
            // 게임 모드: 팝업
            removePopUp?.OnOpen(context.Item);
        }
    }

    /// <summary>
    /// 퀵슬롯에서 제거 (인벤토리 데이터는 유지)
    /// </summary>
    private void HandleQuickSlotRemoval(DragContext context)
    {
        QuickSlot quickSlot = FindFirstObjectByType<QuickSlot>();

        if (quickSlot == null)
        {
            Debug.LogError("[WorldDropZone] QuickSlot을 찾을 수 없습니다!");
            return;
        }

        int slotIndex = context.SourceSlotIndex;

        if (slotIndex >= 0)
        {
            quickSlot.RemoveSlot(slotIndex);
            Debug.Log($"[WorldDropZone] 퀵슬롯 {slotIndex}에서 {context.Item.itemName} 제거 (인벤토리는 유지)");
        }
        else
        {
            Debug.LogWarning("[WorldDropZone] 유효하지 않은 퀵슬롯 인덱스입니다!");
        }
    }
    // OnDrop은 IDropTarget으로 위임
    //public void OnDrop(PointerEventData eventData)
    //{
    //    var context = ItemIconDragHandler.CurrentContext;

    //    if (context == null || !CanAcceptDrop(context))
    //    {
    //        Debug.Log("[WorldDropZone] 드롭 거부됨");
    //        return;
    //    }

    //    HandleDrop(context);
    //}

    //public async void OnDrop(PointerEventData eventData)
    //{
    //    ItemBase draggedItem = ItemIconDragHandler.CurrentDraggedItem;

    //    if (draggedItem == null)
    //    {
    //        Debug.LogWarning("[WorldDropZone] 드래그한 아이템이 없습니다.");
    //        return;
    //    }

    //    lastDroppedItem = draggedItem;

    //    if (playerTransform == null)
    //    {
    //        Debug.LogError("[WorldDropZone] 플레이어 Transform을 찾을 수 없습니다!");
    //        return;
    //    }

    //    if (itemSpawner == null)
    //    {
    //        Debug.LogError("[WorldDropZone] ItemSpawner를 찾을 수 없습니다!");
    //        return;
    //    }

    //    try
    //    {
    //        bool isFromDebugInventory = IsFromDebugInventory(eventData);
    //        bool isFromQuickSlot = IsFromQuickSlot(eventData);

    //        if (isFromDebugInventory)
    //        {
    //            // 디버그 모드: 즉시 드롭
    //            Vector3 dropPosition = CalculateDropPosition();
    //            await HandleDebugDrop(draggedItem, dropPosition);
    //        }
    //        else if (isFromQuickSlot)
    //        {
    //            // 퀵슬롯에서 드래그: 무시
    //            Debug.Log("[WorldDropZone] 퀵슬롯에서의 드래그는 무시됩니다.");
    //        }
    //        else
    //        {
    //            // 게임 인벤토리에서 드래그: 팝업 열기
    //            if (removePopUp != null)
    //            {
    //                removePopUp.OnOpen(lastDroppedItem);
    //                Debug.Log("[WorldDropZone] 수량 선택 팝업 열림");
    //            }
    //            else
    //            {
    //                Debug.LogError("[WorldDropZone] RemovePopUp이 할당되지 않았습니다!");
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogError($"[WorldDropZone] 아이템 드롭 중 오류 발생: {ex.Message}");
    //    }

    //    if (dropIndicator != null)
    //    {
    //        dropIndicator.SetActive(false);
    //    }
    //}
    #endregion

    #region RemovePopUp에서 호출
    /// <summary>
    /// 팝업에서 확정된 수량으로 아이템 드롭
    /// </summary>
    public async void DropItemFromPopup(ItemBase item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            Debug.LogWarning("[WorldDropZone] 유효하지 않은 아이템 또는 수량입니다.");
            return;
        }

        Vector3 dropPosition = CalculateDropPosition();
        await HandleGameDrop(item, dropPosition, quantity);

        lastDroppedItem = null;
    }
    #endregion


    //#region Drop Handling
    ///// <summary>
    /////  디버그 인벤토리에서 드롭한 것인지 확인
    ///// </summary>
    //private bool IsFromDebugInventory(PointerEventData eventData)
    //{
    //    // 드롭된 아이템이 디버그 인벤토리에서 온 것인지 확인
    //    if (eventData.pointerDrag != null)
    //    {
    //        Transform current = eventData.pointerDrag.transform;
    //        while (current != null)
    //        {
    //            if (current.GetComponent<DebugInventoryMarker>() != null)
    //            {
    //                return true;
    //            }
    //            current = current.parent;
    //        }
    //    }

    //    return false;
    //}
    //private bool IsFromQuickSlot(PointerEventData eventData)
    //{
    //    // 드롭된 아이템이 퀵슬롯에서 온 것인지 확인
    //    if (eventData.pointerDrag != null)
    //    {
    //        Transform current = eventData.pointerDrag.transform;
    //        while (current != null)
    //        {
    //            if (current.GetComponent<QuickSlotUI>() != null)
    //            {
    //                return true;
    //            }
    //            current = current.parent;
    //        }
    //    }

    //    return false;
    //}

    /// <summary>
    /// 게임 모드: 인벤토리에서 드롭 (수량 지정)
    /// </summary>
    private async UniTask HandleGameDrop(ItemBase draggedItem, Vector3 dropPosition, int quantity)
    {
        if (inventoryItemData == null)
        {
            Debug.LogError("[WorldDropZone] InventoryItemData를 찾을 수 없습니다!");
            return;
        }

        // 실제 인벤토리 수량 확인 (이미 RemovePopUp에서 제거했으므로 체크만)
        int currentCount = inventoryItemData.GetItemCount(draggedItem.itemID);

        // 아이템 월드에 생성 (설정한 수량 만큼)
        if (itemSpawner != null)
        {
            await itemSpawner.DropItemWithQuantity(draggedItem, dropPosition, quantity);
            InventroyEventSystem.ItemDropped();
            Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{quantity}개를 월드에 Stack 드롭했습니다.");
        }
        else
        {
            Debug.LogError("[WorldDropZone] ItemSpawner를 찾을 수 없습니다!");
        }
        Debug.Log($"[WorldDropZone] 드롭 후 인벤토리 남은 수량: {currentCount}");
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
            await itemSpawner.DropItemWithQuantity(draggedItem, dropPosition, 1); //TODO: 설정한 수량에 맞게 stack 생성
        }
        InventroyEventSystem.ItemDropped();
        Debug.Log($"[WorldDropZone] {draggedItem.itemName} x{debugSpawnCount}개를 드롭했습니다. (디버그 모드)");
    }
    //#endregion

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
    #endregion
}