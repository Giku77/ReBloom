using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace IndieImpulseAssets
{
    public class ButtonMouseOverScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Material hoverMaterial;
        public Image Effect;
        private Material currentMaterial;
        private void Start()
        {
            currentMaterial = Effect.material;
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            Effect.material = hoverMaterial;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Effect.material = currentMaterial;
        }
    }
}