using UnityEngine;
using UnityEngine.UI;
namespace IndieImpulseAssets
{
    public class TCWithBG : MonoBehaviour
    {
        public Image BackgroundImage;        // Reference to the UI Image (if you are changing the color of a UI element)
        [Range(0f, 1f)]
        public float controlledValue;
        private void Start()
        {
            controlledValue = 1;
        }

        private void Update()
        {
            Color color = BackgroundImage.color;
            color.a = controlledValue;
            BackgroundImage.color = color;

            Color transparencyColor = Color.Lerp(Color.black, Color.white, controlledValue);

            // Apply the new color to the material of the Renderer component

            GetComponent<Image>().material.SetColor("_Transparency", transparencyColor);
        }
        private void OnDisable()
        {
            GetComponent<Image>().material.SetColor("_Transparency", Color.white);
        }
    }
}