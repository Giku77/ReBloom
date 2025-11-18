using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 퀵슬롯 개별 슬롯에 붙이는 드롭존
/// 인벤토리에서 드래그한 아이템을 퀵슬롯에 할당
/// </summary>
[RequireComponent(typeof(Image))] // 레이캐스트를 위해 Image 필요
public class QuickSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private QuickSlot quickSlotManager; // QuickSlot 매니저
    [SerializeField] private int slotIndex; // 이 슬롯의 인덱스 (0~5)

    [Header("Visual Feedback")]
    [SerializeField] private Image backgroundImage; // 배경 이미지
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.8f, 1f, 0.8f, 1f); // 연한 초록
    [SerializeField] private Color invalidColor = new Color(1f, 0.8f, 0.8f, 1f); // 연한 빨강

    private bool isPointerOver = false;

    #region Unity Lifecycle
    private void Awake()
    {
        if (quickSlotManager == null)
        {
            Debug.Log("QuickSlot이 인스펙터에서 할당되지 않았습니다");
        }

        // 배경 이미지 자동 찾기
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // 초기 색상 설정
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    private void Update()
    {
        // 드래그 중일 때만 시각 피드백
        if (isPointerOver && ItemIconDragHandler.CurrentDraggedItem != null)
        {
            UpdateVisualFeedback();
        }
    }
    #endregion

    #region Event Handlers
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        // 드래그 중이면 시각 피드백
        if (ItemIconDragHandler.CurrentDraggedItem != null)
        {
            UpdateVisualFeedback();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        // 원래 색상으로 복원
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemBase draggedItem = ItemIconDragHandler.CurrentDraggedItem;

        if (draggedItem == null)
        {
            Debug.LogWarning("[QuickSlotDropZone] 드래그된 아이템이 없습니다.");
            return;
        }

        // 유효성 검증
        if (!CanAssignToQuickSlot(draggedItem))
        {
            //Debug.LogWarning($"[QuickSlotDropZone] {draggedItem.itemName}은(는) 퀵슬롯에 배치할 수 없습니다.");
            ShowInvalidFeedback();
            return;
        }

        // 퀵슬롯에 할당
        AssignToQuickSlot(draggedItem);

        // 색상 복원
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
        Debug.Log("퀵슬롯 배치 완료");
    }
    #endregion

    #region Assignment Logic
    /// <summary>
    /// 아이템을 퀵슬롯에 할당
    /// </summary>
    private void AssignToQuickSlot(ItemBase item)
    {
        if (quickSlotManager == null)
        {
            Debug.LogError("[QuickSlotDropZone] QuickSlot 매니저를 찾을 수 없습니다!");
            return;
        }

        // 인벤토리에서 수량 가져오기 (GameInventory를 통해)
        GameInventory gameInventory = FindFirstObjectByType<GameInventory>();
        int quantity = gameInventory != null ? gameInventory.GetItemCount(item.itemID) : 0;

        if (quantity <= 0)
        {
            Debug.LogWarning($"[QuickSlotDropZone] 인벤토리에 {item.itemName}이(가) 없습니다!");
            return;
        }

        // 기존 슬롯에 아이템이 있는지 확인
        ItemBase existingItem = quickSlotManager.GetItemAtSlot(slotIndex);

        if (existingItem != null)
        {
            // 교체: 기존 아이템 제거 후 새 아이템 할당
            Debug.Log($"[QuickSlotDropZone] 슬롯 {slotIndex}의 {existingItem.itemName}을(를) {item.itemName}(으)로 교체합니다.");

            quickSlotManager.RemoveSlot(slotIndex);
        }

        // 새 아이템 할당
        bool success = AssignToSpecificSlot(item, quantity, slotIndex);

        if (success)
        {
            Debug.Log($"[QuickSlotDropZone] {item.itemName} x{quantity}를 퀵슬롯 {slotIndex}에 배치했습니다.");
        }
        else
        {
            Debug.LogWarning($"[QuickSlotDropZone] 퀵슬롯 할당 실패!");
        }
    }

    /// <summary>
    /// 특정 슬롯에 직접 할당 (QuickSlot 확장 필요)
    /// </summary>
    private bool AssignToSpecificSlot(ItemBase item, int quantity, int targetSlot)
    {
        return quickSlotManager.AssignToSlot(targetSlot, item, quantity);
    }
    #endregion

    #region Validation
    /// <summary>
    /// 퀵슬롯에 배치 가능한지 검증
    /// </summary>
    private bool CanAssignToQuickSlot(ItemBase item)
    {
        if (item == null) return false;

        // 1. 퀵슬롯 플래그 체크
        if (!item.canQuickSlot)
        {
            //Debug.LogWarning($"[QuickSlotDropZone] {item.itemName}은(는) canQuickSlot이 false입니다.");
            return false;
        }

        // 2. 인벤토리에 아이템이 있는지 확인
        GameInventory gameInventory = FindFirstObjectByType<GameInventory>();
        if (gameInventory != null && !gameInventory.HasItem(item.itemID, 1))
        {
            Debug.LogWarning($"[QuickSlotDropZone] 인벤토리에 {item.itemName}이(가) 없습니다.");
            return false;
        }

        // 3. 이미 다른 슬롯에 배치되어 있는지 확인
        if (quickSlotManager.IsItemAlreadyAssigned(item))
        {
            int existingSlot = quickSlotManager.FindItemSlot(item);

            // 같은 슬롯에 드롭하면 무시
            if (existingSlot == slotIndex)
            {
                Debug.Log($"[QuickSlotDropZone] 같은 슬롯에 드롭 (무시)");
                return false;
            }

            // 다른 슬롯에 있으면 이동 허용
            Debug.Log($"[QuickSlotDropZone] {item.itemName}을(를) 슬롯 {existingSlot}에서 {slotIndex}(으)로 이동합니다.");

            // 기존 슬롯에서 제거
            quickSlotManager.RemoveSlot(existingSlot);
        }

        return true;
    }
    #endregion

    #region Visual Feedback
    /// <summary>
    /// 시각 피드백 업데이트
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (backgroundImage == null) return;

        ItemBase draggedItem = ItemIconDragHandler.CurrentDraggedItem;

        if (draggedItem != null && CanAssignToQuickSlot(draggedItem))
        {
            // 배치 가능: 초록색
            backgroundImage.color = hoverColor;
        }
        else
        {
            // 배치 불가: 빨간색
            backgroundImage.color = invalidColor;
        }
    }

    /// <summary>
    /// 잘못된 드롭 피드백
    /// </summary>
    private void ShowInvalidFeedback()
    {
        if (backgroundImage == null) return;

        // 빨간색으로 깜빡임
        backgroundImage.color = invalidColor;
        Invoke(nameof(ResetColor), 0.3f);
    }

    private void ResetColor()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
    #endregion

    #region Inspector Helper
    /// <summary>
    /// Inspector에서 슬롯 인덱스 자동 설정
    /// </summary>
    [ContextMenu("Auto Set Slot Index")]
    private void AutoSetSlotIndex()
    {
        slotIndex = transform.GetSiblingIndex();
        Debug.Log($"[QuickSlotDropZone] 슬롯 인덱스 자동 설정: {slotIndex}");
    }
    #endregion
}