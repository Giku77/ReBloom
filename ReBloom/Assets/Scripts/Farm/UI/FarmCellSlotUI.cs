using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FarmCellSlotUI : MonoBehaviour, IPointerClickHandler, IDropTarget
{
    [SerializeField] private Image stageIcon;
    [SerializeField] private Image highlight;
    [SerializeField] private TextMeshProUGUI stateText;

    private int cellIndex;
    private CropSlot slot;        
    private Action onClick;
    private Action<int> onSeedDrop;

    public void Bind(int index, CropSlot slot, Action onClick, Action<int> onSeedDrop)
    {
        cellIndex = index;
        this.slot = slot;
        this.onClick = onClick;
        this.onSeedDrop = onSeedDrop;
        Refresh(slot);
    }

    public void Refresh(CropSlot slot)
    {
        this.slot = slot;

        if (highlight) highlight.enabled = false;

        if (stateText)
        {
            if (slot == null || slot.state == CropSlotState.Empty) stateText.text = "빈 칸";
            else if (slot.state == CropSlotState.Mature) stateText.text = "수확 가능";
            else if (slot.state == CropSlotState.Growing) stateText.text = "성장 중";
            else stateText.text = "시듦";
        }

        // stageIcon 갱신도 여기서:
        // - slot.crop / slot.stageIndex 기반으로 Addressables 스프라이트 로딩해서 넣으면 됨
    }

    public void SetSelected(bool selected)
    {
        if (highlight) highlight.enabled = selected;
    }

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke();

    public bool CanAcceptDrop(DragContext context)
    {
        if (context?.Item == null) return false;
        if (context.SourceType != DragSourceType.SeedList) return false;

        // 빈칸에만 심게 하려면:
        if (slot != null && slot.state != CropSlotState.Empty) return false;

        return true;
    }

    public void HandleDrop(DragContext context)
    {
        Debug.Log($"[FarmCellSlotUI] HandleDrop called for cell {cellIndex}");
        int seedId = context.Item.itemID;
        if (seedId <= 0) return;

        onSeedDrop?.Invoke(seedId);
    }
}
