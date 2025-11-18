using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildToolTip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;          // 같은 Canvas
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Vector2 offset = new Vector2(10f, -10f); // 마우스에서 살짝 띄우기

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        Hide();
    }

    public void Show(string info, Vector2 screenPos)
    {
        infoText.text = info;

        gameObject.SetActive(true);
        SetPosition(screenPos);
    }

    public void SetPosition(Vector2 screenPos)
    {
        // Screen 좌표 → Canvas 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out var localPoint
        );

        rectTransform.anchoredPosition = localPoint + offset;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
