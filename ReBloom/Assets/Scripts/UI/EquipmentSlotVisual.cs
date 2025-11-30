using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotVisual : UIBase
{
    [Header("UI Elements")]
    [SerializeField] private Image clothImage;
    [SerializeField] private Image shoesImage;
    
    [Header("References")]
    [SerializeField] private PlayerEquipManager equipManager;
    [SerializeField] private PlayerEquipData equipData;
    
    [Header("Color Settings")]
    [SerializeField] private Color equippedColor = Color.white;
    [SerializeField] private Color unequippedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    private void Start()
    { 
        UpdateVisuals();
    }
    
    private void Update()
    {
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (equipData == null) return;
        
        if (clothImage != null)
        {
            if (equipData.currentClothEquip != null)
            {
                clothImage.color = equippedColor;
            }
            else
            {
                clothImage.color = unequippedColor;
            }
        }

        if (shoesImage != null)
        {
            if (equipData.currentShoesEquip != null)
            {
                shoesImage.color = equippedColor;
            }
            else
            {
                shoesImage.color = unequippedColor;
            }
        }
    }
}