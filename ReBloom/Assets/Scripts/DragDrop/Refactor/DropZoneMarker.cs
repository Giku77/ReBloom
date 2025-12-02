using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드롭 가능 영역 식별자 (데이터만)
/// 모든 로직은 DragDropManager에서 처리
/// </summary>
public class DropZoneMarker : MonoBehaviour
{
    [Header("Drop Zone Settings")]
    [SerializeField] private DropZoneType zoneType;
    [SerializeField] private int slotIndex = -1;
    [SerializeField] private int priority = 50;

    [Header("Visual Feedback")]
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.8f, 0.8f, 1f);

    // Getter만 제공
    public DropZoneType ZoneType => zoneType;
    public int SlotIndex => slotIndex;
    public int Priority => priority;

    #region 비주얼 피드백 (UI만)
    public void ShowValidHover()
    {
        if (background != null)
            background.color = hoverColor;
    }

    public void ShowInvalidHover()
    {
        if (background != null)
            background.color = invalidColor;
    }

    public void ResetVisual()
    {
        if (background != null)
            background.color = normalColor;
    }
    #endregion

    #region Context Menu
    [ContextMenu("Auto Set Slot Index")]
    private void AutoSetSlotIndex()
    {
        slotIndex = transform.GetSiblingIndex();
        Debug.Log($"[DropZoneMarker] 슬롯 인덱스 자동 설정: {slotIndex}");
    }

    [ContextMenu("Set Quick Slot Priority")]
    private void SetQuickSlotPriority()
    {
        zoneType = DropZoneType.QuickSlot;
        priority = 100;
    }

    [ContextMenu("Set Storage Priority")]
    private void SetStoragePriority()
    {
        zoneType = DropZoneType.Storage;
        priority = 50;
    }

    [ContextMenu("Set World Priority")]
    private void SetWorldPriority()
    {
        zoneType = DropZoneType.World;
        priority = 10;
    }
    #endregion

    private void Awake()
    {
        if (background == null)
        {
            background = GetComponent<Image>();
        }

        if (background != null)
        {
            normalColor = background.color;
        }
    }
}