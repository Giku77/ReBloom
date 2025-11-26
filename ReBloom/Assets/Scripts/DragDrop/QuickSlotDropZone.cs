using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class QuickSlotDropZone : MonoBehaviour,
    IDropTarget, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private QuickSlot quickSlotManager;
    [SerializeField] private int slotIndex;

    [Header("Visual Feedback")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color originColor;
    [SerializeField] private Color hoverColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.8f, 0.8f, 1f);

    private bool isPointerOver = false;

    private void Awake()
    {
        if (quickSlotManager == null)
        {
            quickSlotManager = FindFirstObjectByType<QuickSlot>();
            if (quickSlotManager == null)
            {
                Debug.LogError("[QuickSlotDropZone] QuickSlot 매니저를 찾을 수 없습니다!");
            }
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (backgroundImage != null)
        {
            originColor = backgroundImage.color;
        }
    }

    private void Update()
    {
        // 수정
        if (isPointerOver && ItemIconDragHandler.CurrentContext?.Item != null)
        {
            UpdateVisualFeedback();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        // 수정
        if (ItemIconDragHandler.CurrentContext?.Item != null)
        {
            UpdateVisualFeedback();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        if (backgroundImage != null)
        {
            backgroundImage.color = originColor;
        }
    }

    #region IDropTarget 구현
    /// <summary>
    /// 이 퀵슬롯이 드롭을 받을 수 있는지 판단
    /// </summary>
    public bool CanAcceptDrop(DragContext context)
    {
        if (context?.Item == null) return false;

        // 같은 슬롯에 드롭: 거부
        if (context.IsFromQuickSlot && context.SourceSlotIndex == slotIndex)
        {
            return false;
        }

        // 아이템 유효성 검증
        return CanAssignToQuickSlot(context);
    }

    /// <summary>
    /// 실제 드롭 처리
    /// </summary>
    public void HandleDrop(DragContext context)
    {
        if (context.IsFromQuickSlot)
        {
            // 퀵슬롯 간 스왑
            SwapQuickSlots(context.SourceSlotIndex, slotIndex);
        }
        else
        {
            // 인벤토리에서 퀵슬롯 배치
            AssignToQuickSlot(context.Item);
        }

        // 색상 복원
        if (backgroundImage != null)
        {
            backgroundImage.color = originColor;
        }
    }
    #endregion

    #region Unity IDropHandler (위임)
    public void OnDrop(PointerEventData eventData)
    {
        var context = ItemIconDragHandler.CurrentContext;

        if (context == null)
        {
            Debug.LogWarning("[QuickSlotDropZone] 드래그 컨텍스트가 없습니다.");
            return;
        }

        // 같은 슬롯 체크는 CanAcceptDrop에서
        if (!CanAcceptDrop(context))
        {
            Debug.Log($"[QuickSlotDropZone] 드롭 거부됨");
            ShowInvalidFeedback();
            return;
        }

        HandleDrop(context);
    }
    #endregion

    #region 퀵슬롯 로직
    /// <summary>
    /// 퀵슬롯 간 스왑 (A ↔ B)
    /// </summary>
    private void SwapQuickSlots(int fromIndex, int toIndex)
    {
        if (quickSlotManager == null) return;

        ItemBase fromItem = quickSlotManager.GetItemAtSlot(fromIndex);
        ItemBase toItem = quickSlotManager.GetItemAtSlot(toIndex);

        GameInventory gameInventory = FindFirstObjectByType<GameInventory>();
        if (gameInventory == null)
        {
            Debug.LogError("[QuickSlotDropZone] GameInventory를 찾을 수 없습니다!");
            return;
        }

        int fromQuantity = gameInventory.GetItemCount(fromItem.itemID);
        int toQuantity = toItem != null ? gameInventory.GetItemCount(toItem.itemID) : 0;

        Debug.Log($"[QuickSlotDropZone] 스왑: [{fromIndex}] {fromItem.itemName} <-> [{toIndex}] {(toItem != null ? toItem.itemName : "빈 슬롯")}");

        // 1. 두 슬롯 모두 제거
        quickSlotManager.RemoveSlot(fromIndex);
        if (toItem != null)
        {
            quickSlotManager.RemoveSlot(toIndex);
        }

        // 2. 교차 배치
        quickSlotManager.AssignToSlot(toIndex, fromItem, fromQuantity);

        if (toItem != null)
        {
            quickSlotManager.AssignToSlot(fromIndex, toItem, toQuantity);
        }

        Debug.Log($"[QuickSlotDropZone] 스왑 완료!");
    }

    /// <summary>
    /// 인벤토리에서 퀵슬롯 배치
    /// </summary>
    private void AssignToQuickSlot(ItemBase item)
    {
        if (quickSlotManager == null)
        {
            Debug.LogError("[QuickSlotDropZone] QuickSlot 매니저를 찾을 수 없습니다!");
            return;
        }

        GameInventory gameInventory = FindFirstObjectByType<GameInventory>();
        int quantity = gameInventory != null ? gameInventory.GetItemCount(item.itemID) : 0;

        if (quantity <= 0)
        {
            Debug.LogWarning($"[QuickSlotDropZone] 인벤토리에 {item.itemName}이(가) 없습니다!");
            return;
        }

        // 이미 다른 슬롯에 등록되어 있는지 확인
        int existingSlotIndex = quickSlotManager.FindItemSlot(item);

        if (existingSlotIndex >= 0 && existingSlotIndex != slotIndex)
        {
            Debug.Log($"[QuickSlotDropZone] {item.itemName}이(가) 슬롯 {existingSlotIndex}에 있어서 제거 후 슬롯 {slotIndex}에 재배치");
            quickSlotManager.RemoveSlot(existingSlotIndex);
        }

        // 현재 슬롯에 기존 아이템 있으면 교체
        ItemBase currentSlotItem = quickSlotManager.GetItemAtSlot(slotIndex);

        if (currentSlotItem != null)
        {
            Debug.Log($"[QuickSlotDropZone] 슬롯 {slotIndex}: {currentSlotItem.itemName} → {item.itemName} (교체)");
            quickSlotManager.RemoveSlot(slotIndex);
        }

        // 새 아이템 배치
        bool success = quickSlotManager.AssignToSlot(slotIndex, item, quantity);

        if (success)
        {
            Debug.Log($"[QuickSlotDropZone] {item.itemName} x{quantity}를 슬롯 {slotIndex}에 배치 완료");
        }
        else
        {
            Debug.LogWarning($"[QuickSlotDropZone] 배치 실패!");
        }
    }
    #endregion

    #region 유효성 검증
    /// <summary>
    /// 퀵슬롯에 배치 가능한지 검증
    /// </summary>
    private bool CanAssignToQuickSlot(DragContext context)
    {
        if (context?.Item == null) return false;

        // 1. canQuickSlot 플래그 체크
        if (!context.Item.canQuickSlot)
        {
            //Debug.LogWarning($"[QuickSlotDropZone] {context.Item.itemName}은(는) 퀵슬롯에 배치할 수 없습니다.");
            return false;
        }

        // 2. 인벤토리에 아이템 있는지 확인
        GameInventory gameInventory = FindFirstObjectByType<GameInventory>();
        if (gameInventory != null && !gameInventory.HasItem(context.Item.itemID, 1))
        {
            Debug.LogWarning($"[QuickSlotDropZone] 인벤토리에 {context.Item.itemName}이(가) 없습니다.");
            return false;
        }

        // 3. 퀵슬롯 간 이동: 항상 허용
        if (context.IsFromQuickSlot)
        {
            return true;
        }

        // 4. 인벤토리에서 퀵슬롯: 허용
        return true;
    }
    #endregion

    #region 비주얼 피드백
    private void UpdateVisualFeedback()
    {
        if (backgroundImage == null) return;

        var context = ItemIconDragHandler.CurrentContext;

        if (context != null && CanAcceptDrop(context))
        {
            backgroundImage.color = hoverColor;
        }
        else
        {
            backgroundImage.color = invalidColor;
        }
    }

    private void ShowInvalidFeedback()
    {
        if (backgroundImage == null) return;

        backgroundImage.color = invalidColor;
        Invoke(nameof(ResetColor), 0.3f);
    }

    private void ResetColor()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = originColor;
        }
    }
    #endregion

    [ContextMenu("Auto Set Slot Index")]
    private void AutoSetSlotIndex()
    {
        slotIndex = transform.GetSiblingIndex();
        Debug.Log($"[QuickSlotDropZone] 슬롯 인덱스 자동 설정: {slotIndex}");
    }
}