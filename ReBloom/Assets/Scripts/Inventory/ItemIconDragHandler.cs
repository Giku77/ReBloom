using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemIconDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage; // 아이템 아이콘 이미지

    [Header("Drag Settings")]
    [SerializeField] private float dragAlpha = 0.6f; // 드래그 중 투명도

    private Canvas canvas; // UI가 속한 캔버스
    private ItemBase itemData; // 현재 아이템 데이터
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;

    // 드래그 하고있는 아이템을 외부에 알리기 위함
    public static ItemBase CurrentDraggedItem { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // CanvasGroup이 없으면 추가 (투명도 조절용)
        if(canvasGroup == null)
        canvasGroup = GetComponent<CanvasGroup>(); //@ 아이템 슬롯에 붙이는게 맞는지 확인필요
    }

    /// <summary>
    /// 이 아이콘이 표현할 아이템 데이터를 설정, @ 커서에 따라다닐 아이템 데이터 셋
    /// </summary>
    public void SetItemData(ItemBase data)
    {
       itemData = data;
        if(iconImage != null && data != null)
        {
            iconImage.sprite = data.icon;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

        originalPosition = rectTransform.anchoredPosition;
        transform.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;

        CurrentDraggedItem = itemData;

        rectTransform.SetAsLastSibling(); // 드래그를 맨 앞으로
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

        // 마우스 위치를 따라 이동
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

        // 원래 부모로 되돌리기
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        // 드래그 중 끈 raycast 다시 켜기
        canvasGroup.blocksRaycasts = true;

        // 투명도 복원
        canvasGroup.alpha = 1f;

        // 원래 위치로 복원
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;

        // 드래그 정보 초기화
        CurrentDraggedItem = null;

        Debug.Log($"드래그 종료: {itemData.itemName}");
    }
}
