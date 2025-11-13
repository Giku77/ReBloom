using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuickSlotUI : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] Image slotIcon;
    [SerializeField] TextMeshProUGUI itemQuantity;

    [Header("Fallback")]
    [SerializeField] private Sprite defaultIcon; // 기본 아이콘 (Optional)

    /// <summary>
    /// 슬롯 정보 업데이트
    /// </summary>
    public void OnUpdateSlotInfo(ItemBase currentSlotData, int quantity)
    {
        if (currentSlotData == null)
        {
            Debug.LogError("[QuickSlotUI] currentSlotData가 null입니다!");
            return;
        }

        if (slotIcon != null)
        {
            if (currentSlotData.icon != null)
            {
                slotIcon.sprite = currentSlotData.icon;
                slotIcon.enabled = true;
                slotIcon.color = Color.white;
            }
            else
            {
                // 아이콘이 없으면 기본 아이콘 사용 또는 비활성화
                if (defaultIcon != null)
                {
                    slotIcon.sprite = defaultIcon;
                    slotIcon.enabled = true;
                    slotIcon.color = Color.gray;  // 로딩 중 표시
                    Debug.LogWarning($"[QuickSlotUI] {currentSlotData.itemName} 아이콘이 없어 기본 아이콘 사용");
                }
                else
                {
                    slotIcon.enabled = false;
                    Debug.LogWarning($"[QuickSlotUI] {currentSlotData.itemName} 아이콘 없음 (비활성화)");
                }
            }
        }

        if (itemQuantity != null)
        {
            itemQuantity.text = quantity.ToString();
        }
    }
}