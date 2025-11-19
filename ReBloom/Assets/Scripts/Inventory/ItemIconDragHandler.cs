using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemIconDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;

    private Canvas canvas;
    private ItemBase itemData;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // 원래 위치 정보
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;

    // 드래그 시작 위치 판단용
    private bool isDraggingFromQuickSlot;
    private bool isDraggingFromInventory;

    // 정적 변수
    public static ItemBase CurrentDraggedItem { get; private set; }
    public static int CurrentDraggedSlotIndex { get; private set; } = -1;

    #region 유니티 생명주기
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup = GetComponent<CanvasGroup>();
    }
    #endregion

    #region 데이터 셋
    public void SetItemData(ItemBase data)
    {
        itemData = data;
        if (iconImage != null && data != null)
        {
            iconImage.sprite = data.icon;
        }
    }
    #endregion

    #region 드래그앤 드롭
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

        // 원래 정보 저장
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 어디서 드래그 시작했는지 확인
        isDraggingFromQuickSlot = originalParent.GetComponentInParent<QuickSlotDropZone>() != null;
        isDraggingFromInventory = originalParent.GetComponentInParent<GameInventoryUI>() != null;

        // 퀵슬롯 인덱스 저장
        if (isDraggingFromQuickSlot)
        {
            CurrentDraggedSlotIndex = originalParent.GetSiblingIndex();
            Debug.Log($"[DragHandler] 퀵슬롯 {CurrentDraggedSlotIndex}에서 드래그 시작");
        }
        else
        {
            CurrentDraggedSlotIndex = -1;
        }

        // 드래그 설정
        transform.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;

        CurrentDraggedItem = itemData;
        rectTransform.SetAsLastSibling();

        Debug.Log($"[DragHandler] 드래그 시작: {itemData.itemName} (퀵슬롯: {isDraggingFromQuickSlot}, 인벤토리: {isDraggingFromInventory})");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemData == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

        // 드롭 성공 여부 확인
        bool droppedOnValidSlot = eventData.pointerEnter != null &&
                                  eventData.pointerEnter.GetComponent<QuickSlotDropZone>() != null;

        //if (!droppedOnValidSlot)
        //{
        //    // 유효하지 않은 곳에 드롭: 원래 위치로 복구
        //    Debug.Log($"[DragHandler] 잘못된 위치 드롭 - 원래 위치로 복구");
        //    transform.SetParent(originalParent, true);
        //    transform.SetSiblingIndex(originalSiblingIndex);
        //    rectTransform.anchoredPosition = originalPosition;
        //}
        if(droppedOnValidSlot)
        {
            // 유효한 드롭 성공
            Debug.Log($"[DragHandler] 유효한 드롭 완료");

            // 인벤토리에서 드래그한 경우: 인벤토리 UI 새로고침
            if (isDraggingFromInventory)
            {
                RefreshInventoryUI();
            }

            // 기존 UI 파괴 (새 UI가 생성되므로)
            Destroy(gameObject);
        }

        // 상태 복원
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // 정적 변수 초기화
        CurrentDraggedItem = null;
        CurrentDraggedSlotIndex = -1;
    }

    /// <summary>
    /// 인벤토리 UI 강제 새로고침
    /// </summary>
    private void RefreshInventoryUI()
    {
        GameInventoryUI inventoryUI = FindFirstObjectByType<GameInventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
            Debug.Log("[DragHandler] 인벤토리 UI 새로고침 완료");
        }
        else
        {
            Debug.LogWarning("[DragHandler] GameInventoryUI를 찾을 수 없습니다!");
        }
    }
    #endregion
}