using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CultivationCellSlotUI : MonoBehaviour, IPointerClickHandler, IDropTarget, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image highlight;  
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI stateText;

    private CultivationMachine machine;
    private Action onClick;
    private Action<int> onSeedDrop;

    private Color baseBgColor;

    private void Awake()
    {
        if (highlight) baseBgColor = highlight.color;
        if (icon) icon.enabled = false;
    }

    public void Bind(CultivationMachine machine, Action onClick, Action<int> onSeedDrop)
    {
        this.machine = machine;
        this.onClick = onClick;
        this.onSeedDrop = onSeedDrop;
        Refresh();
    }

    public void Refresh()
    {
        var slot = machine != null ? machine.Slot : null;

        // 상태 텍스트
        if (stateText)
        {
            if (slot == null || slot.state == CultivationSlotState.Empty) stateText.text = "빈 칸";
            else if (slot.state == CultivationSlotState.Running) stateText.text = "가동 중";
            else stateText.text = "수거 가능";
        }

        if (icon)
        {
            bool shouldShowIcon = (slot != null && slot.state != CultivationSlotState.Empty);
            icon.enabled = shouldShowIcon;

            // if (shouldShowIcon) icon.sprite = ...;
        }

        if (highlight) highlight.color = baseBgColor;
    }

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight)
        {
            var c = baseBgColor;      
            c.a = 0.8f;         
            highlight.color = c;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight) highlight.color = baseBgColor;
    }

    public bool CanAcceptDrop(DragContext context)
    {
        if (machine == null) return false;
        if (context?.Item == null) return false;
        if (context.SourceType != DragSourceType.SeedList) return false;

        var slot = machine.Slot;
        if (slot != null && slot.state != CultivationSlotState.Empty) return false;

        return true;
    }

    public void HandleDrop(DragContext context)
    {
        int itemId = context.Item.itemID;
        if (itemId <= 0) return;

        onSeedDrop?.Invoke(itemId);
    }
}
