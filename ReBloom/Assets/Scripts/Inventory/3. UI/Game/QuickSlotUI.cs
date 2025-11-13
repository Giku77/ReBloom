using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuickSlotUI : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] Image slotIcon;
    [SerializeField] TextMeshProUGUI itemQuantity;
    public void OnUpdateSlotInfo(ItemBase currentSlotData, int quantity)
    {
        slotIcon.sprite = currentSlotData.icon;
        itemQuantity.text = $"{quantity}";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
