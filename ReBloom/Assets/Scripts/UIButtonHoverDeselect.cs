using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverDeselect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static bool IsMouseHoveringButton;

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsMouseHoveringButton = true;

        if (EventSystem.current.currentSelectedGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
         IsMouseHoveringButton = false;
    }
}
