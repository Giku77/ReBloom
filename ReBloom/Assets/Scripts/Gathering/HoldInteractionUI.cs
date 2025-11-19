using UnityEngine;
using UnityEngine.UI;

public class HoldInteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject sliderPanel;
    [SerializeField] private Slider progressSlider;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        sliderPanel.SetActive(true);
        progressSlider.value = 0f;
    }

    public void UpdateProgress(float progress)
    {
        progressSlider.value = progress; // 0~1 사이 값
    }

    public void Hide()
    {
        sliderPanel.SetActive(false);
    }
}
