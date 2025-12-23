using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DefaultButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (button.interactable)
        {
            SoundManager.I?.PlayUIClick();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            SoundManager.I?.PlayHover();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (button.interactable)
        {
            SoundManager.I?.PlayHover();
        }
    }
}
