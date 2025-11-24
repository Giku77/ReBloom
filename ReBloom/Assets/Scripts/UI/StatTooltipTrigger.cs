using UnityEngine;
using UnityEngine.EventSystems;

public class StatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private StatTooltipUI tooltipUI;

    private bool isHovering = false;

    private void Start()
    {
        if (tooltipUI == null)
        {
            tooltipUI = FindFirstObjectByType<StatTooltipUI>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipUI != null)
        {
            isHovering = true;
            tooltipUI.ShowTooltip(eventData.position);
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (tooltipUI != null && isHovering)
        {
            tooltipUI.ShowTooltip(eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipUI != null)
        {
            isHovering = false;
            tooltipUI.HideTooltip();
        }
    }
}