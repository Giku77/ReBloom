using UnityEngine;
using UnityEngine.EventSystems;

public class BuildSlotIconTooltip : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private BuildSlotUI slotUI;

    public void OnPointerEnter(PointerEventData eventData)
    {
        slotUI.OnIconPointerEnter(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        slotUI.OnIconPointerMove(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        slotUI.OnIconPointerExit(eventData);
    }
}
