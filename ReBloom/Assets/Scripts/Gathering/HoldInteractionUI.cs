using UnityEngine;
using UnityEngine.UI;

public class HoldInteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject sliderPanel;
    [SerializeField] private Slider progressSlider;

    //private void Awake()
    //{
    //    Hide();
    //}

    private void Start()
    {
        //Hide();
    }

    public void Show()
    {
        sliderPanel.SetActive(true);
        progressSlider.value = 0f;
    }

    public void UpdateProgress(float progress)
    {
        progressSlider.value = progress;
    }

    public void Hide()
    {
        sliderPanel.SetActive(false);
    }
}
