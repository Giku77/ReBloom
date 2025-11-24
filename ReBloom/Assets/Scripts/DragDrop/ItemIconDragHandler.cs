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

    private IDragSource dragSource;
    public static DragContext CurrentContext { get; private set; }

    #region 유니티 생명주기
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
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

    #region 드래그앤 드롭 인터페이스 구현
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

        // 출발지 인터페이스 찾기
        dragSource = GetComponentInParent<IDragSource>();

        if (dragSource == null)
        {
            Debug.LogError("[DragHandler] IDragSource를 찾을 수 없습니다!");
            return;
        }

        // 컨텍스트 생성
        CurrentContext = dragSource.CreateDragContext(itemData);

        // 드래그 설정
        SaveOriginalState();
        SetupDragVisual();

        Debug.Log($"[DragHandler] 드래그 시작: {itemData.itemName} from {CurrentContext.SourceType}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemData == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (CurrentContext == null) return;

        // 유효한 드롭 타겟인지 확인
        IDropTarget dropTarget = eventData.pointerEnter?.GetComponentInParent<IDropTarget>();

        bool success = dropTarget != null && dropTarget.CanAcceptDrop(CurrentContext);

        if (success)
        {
            // 드롭 타겟이 처리
            dropTarget.HandleDrop(CurrentContext);
            dragSource.OnDragSuccess();

            // UI 처리 판단
            bool shouldDestroyUI = ShouldDestroyUI(dropTarget, CurrentContext);

            if (shouldDestroyUI)
            {
                Destroy(gameObject);
            }
            else
            {
                // 원위치 복원 (디버그, 인벤토리 -> 퀵슬롯)
                RestoreOriginalState();
            }
        }
        else
        {
            // 실패 시 원복
            dragSource.OnDragCancelled();
            RestoreOriginalState();
        }

        // 정리
        ResetDragVisual();
        CurrentContext = null;
    }

    /// <summary>
    /// 드롭 타겟과 컨텍스트에 따라 UI 파괴 여부 결정
    /// </summary>
    private bool ShouldDestroyUI(IDropTarget target, DragContext context)
    {
        // 1. 디버그 슬롯은 항상 유지
        if (context.SourceType == DragSourceType.Debug)
        {
            return false;
        }

        // 2. 인벤토리 → 퀵슬롯: 유지 (참조만)
        if (context.SourceType == DragSourceType.Inventory && target is QuickSlotDropZone)
        {
            return false;
        }

        // 3. 퀵슬롯 → 월드: 파괴 (퀵슬롯에서 제거)
        if (context.SourceType == DragSourceType.QuickSlot && target is WorldDropZone)
        {
            return true;
        }

        // 4. 퀵슬롯 → 퀵슬롯: 파괴 (재생성)
        if (context.SourceType == DragSourceType.QuickSlot && target is QuickSlotDropZone)
        {
            return true;
        }

        // 5. 인벤토리 → 월드: 파괴
        if (context.SourceType == DragSourceType.Inventory && target is WorldDropZone)
        {
            return true;
        }

        // 6. 기본값: 파괴
        return true;
    }
    #endregion

    #region 드래그앤 드롭 관련 함수
    private void SaveOriginalState()
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
    }

    private void RestoreOriginalState()
    {
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalPosition;
    }

    private void SetupDragVisual()
    {
        transform.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.3f;
        rectTransform.SetAsLastSibling();
    }

    private void ResetDragVisual()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
    #endregion
}