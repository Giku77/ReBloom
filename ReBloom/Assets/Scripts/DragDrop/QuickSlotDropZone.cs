using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class QuickSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
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
        if (isPointerOver && ItemIconDragHandler.CurrentDraggedItem != null)
        {
            UpdateVisualFeedback();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        if (ItemIconDragHandler.CurrentDraggedItem != null)
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

    public void OnDrop(PointerEventData eventData)
    {
        ItemBase draggedItem = ItemIconDragHandler.CurrentDraggedItem;
        int draggedFromSlot = ItemIconDragHandler.CurrentDraggedSlotIndex;

        if (draggedItem == null)
        {
            Debug.LogWarning("[QuickSlotDropZone] 드래그된 아이템이 없습니다.");
            return;
        }

        // 같은 슬롯에 드롭: 무시
        if (draggedFromSlot == slotIndex)
        {
            Debug.Log($"[QuickSlotDropZone] 같은 슬롯({slotIndex})에 드롭 - 무시");
            return;
        }

        // 유효성 검증
        if (!CanAssignToQuickSlot(draggedItem, draggedFromSlot))
        {
            ShowInvalidFeedback();
            return;
        }

        // 퀵슬롯 간 이동 vs 인벤토리에서 퀵슬롯 분기
        if (draggedFromSlot >= 0)
        {
            // 퀵슬롯: 퀵슬롯 (스왑)
            SwapQuickSlots(draggedFromSlot, slotIndex);
        }
        else
        {
            // 인벤토리: 퀵슬롯 (배치 또는 교체)
            AssignToQuickSlot(draggedItem);
        }

        // 색상 복원
        if (backgroundImage != null)
        {
            backgroundImage.color = originColor;
        }
    }

    /// <summary>
    /// 퀵슬롯 간 스왑 (A to B / B to A)
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

        Debug.Log($"[QuickSlotDropZone] 스왑 시작: [{fromIndex}] {fromItem.itemName} swap [{toIndex}] {(toItem != null ? toItem.itemName : "빈 슬롯")}");

        // 1️.⃣ 두 슬롯 모두 제거
        quickSlotManager.RemoveSlot(fromIndex);
        if (toItem != null)
        {
            quickSlotManager.RemoveSlot(toIndex);
        }

        // 2️.⃣ 교차 배치
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
            // 다른 슬롯에 이미 있음: 기존 슬롯 제거 후 새 슬롯에 배치
            Debug.Log($"[QuickSlotDropZone] {item.itemName}이(가) 슬롯 {existingSlotIndex}에 있어서 제거 후 슬롯 {slotIndex}에 재배치");
            quickSlotManager.RemoveSlot(existingSlotIndex);
        }

        // 현재 슬롯에 기존 아이템 있으면 교체
        ItemBase currentSlotItem = quickSlotManager.GetItemAtSlot(slotIndex);

        if (currentSlotItem != null)
        {
            Debug.Log($"[QuickSlotDropZone] 슬롯 {slotIndex}: {currentSlotItem.itemName} 에서 {item.itemName} (교체)");
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

    /// <summary>
    /// 유효성 검증 (출발 슬롯 정보 고려)
    /// </summary>
    private bool CanAssignToQuickSlot(ItemBase item, int fromSlotIndex)
    {
        if (item == null) return false;

        // 1️⃣. canQuickSlot 플래그 체크
        if (!item.canQuickSlot)
        {
            Debug.LogWarning($"[QuickSlotDropZone] {item.itemName}은(는) 퀵슬롯에 배치할 수 없습니다.");
            return false;
        }

        // 2️.⃣ 인벤토리에 아이템 있는지 확인
        GameInventory gameInventory = FindFirstObjectByType<GameInventory>();
        if (gameInventory != null && !gameInventory.HasItem(item.itemID, 1))
        {
            Debug.LogWarning($"[QuickSlotDropZone] 인벤토리에 {item.itemName}이(가) 없습니다.");
            return false;
        }

        // 3️.⃣ 퀵슬롯 간 이동인 경우: 항상 허용
        if (fromSlotIndex >= 0)
        {
            return true;
        }

        // 4️⃣ 인벤토리에서 퀵슬롯인 경우
        // 이미 다른 슬롯에 있어도 이동 개념이므로 허용
        // (단, 같은 아이템을 여러 슬롯에 중복 등록하는 건 방지됨 : AssignToQuickSlot에서 처리)
        return true;
    }

    private void UpdateVisualFeedback()
    {
        if (backgroundImage == null) return;

        ItemBase draggedItem = ItemIconDragHandler.CurrentDraggedItem;
        int draggedFromSlot = ItemIconDragHandler.CurrentDraggedSlotIndex;

        if (draggedItem != null && CanAssignToQuickSlot(draggedItem, draggedFromSlot))
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

    [ContextMenu("Auto Set Slot Index")]
    private void AutoSetSlotIndex()
    {
        slotIndex = transform.GetSiblingIndex();
        Debug.Log($"[QuickSlotDropZone] 슬롯 인덱스 자동 설정: {slotIndex}");
    }
}