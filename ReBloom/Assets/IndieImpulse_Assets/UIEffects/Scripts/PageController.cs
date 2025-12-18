using UnityEngine;
using UnityEngine.UI;
namespace IndieImpulseAssets
{
    public class PageController : MonoBehaviour
    {
        public GameObject[] pages; // Drag your pages here in the Unity Inspector
        private int currentPageIndex = 0;

        public Button nextButton;
        public Button backButton;

        public Slider[] sliders;
        public Image[] images;


       

        private void Start()
        {
            UpdateUI();
            for (int i = 0; i < sliders.Length; i++)
            {
                // Using a local method to handle each individual slider
                AddListenerToSlider(i);
            }
            AddListenerToSlider();
        }

        void AddListenerToSlider(int index)
        {
            sliders[index].onValueChanged.AddListener((value) => UpdateImageFill(value, index));
        }

        void AddListenerToSlider()
        {
            sliders[3].onValueChanged.AddListener((value) => UpdateImageFill(value, 4));
        }

        private void UpdateImageFill(float value, int index)
        {
            if (index < images.Length)
                images[index].fillAmount = value;
        }

        public void NextPage()
        {
            if (currentPageIndex < pages.Length - 1)
            {
                pages[currentPageIndex].SetActive(false);
                currentPageIndex++;
                pages[currentPageIndex].SetActive(true);
                UpdateUI();
            }
        }

        public void PreviousPage()
        {
            if (currentPageIndex > 0)
            {
                pages[currentPageIndex].SetActive(false);
                currentPageIndex--;
                pages[currentPageIndex].SetActive(true);
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            nextButton.interactable = currentPageIndex < pages.Length - 1;
            backButton.interactable = currentPageIndex > 0;
        }
    }
}