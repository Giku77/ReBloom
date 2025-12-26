using TMPro;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    public enum PlatformTarget { Both, PCOnly, MobileOnly }

    [Header("Platform Target")]
    [SerializeField] private PlatformTarget targetPlatform = PlatformTarget.Both;

    [Header("Equipment Manager")]
    [SerializeField] private PlayerEquipManager equipManager;
    [SerializeField] private PlayerEquipData equipData;

    [Header("Slot UI References")]
    [SerializeField] private GameObject clothSlotObject;
    [SerializeField] private GameObject shoesSlotObject;
    [SerializeField] private GameObject toolSlotObject;

    [Header("Slot UI Prefab")]
    [SerializeField] private EquipmentSlotUI slotUIPrefab;

    [Header("ResistanceTexts")]
    [SerializeField] private TextMeshProUGUI pollutionResText;
    [SerializeField] private TextMeshProUGUI heightResText;
    [SerializeField] private TextMeshProUGUI moveResText;
    [SerializeField] private PlayerController playerController;

    private EquipmentSlotUI clothSlotUI;
    private EquipmentSlotUI shoesSlotUI;
    private EquipmentSlotUI toolSlotUI;

    private bool IsActiveForCurrentPlatform()
    {
        if (PlatformManager.Instance == null) return true;

        return targetPlatform switch
        {
            PlatformTarget.PCOnly => PlatformManager.Instance.IsPC,
            PlatformTarget.MobileOnly => PlatformManager.Instance.IsMobile,
            PlatformTarget.Both => true,
            _ => true
        };
    }
    private void Awake()
    {
        if (!IsActiveForCurrentPlatform())
        {
            enabled = false;
            return;
        }

        ValidateReferences();
        InitializeSlotUIs();
        if (!IsActiveForCurrentPlatform()) return;
        RefreshAllSlots();
    }
    private void Start()
    {
    }
    private void ValidateReferences()
    {
        if (equipManager == null)
        {
            equipManager = FindAnyObjectByType<PlayerEquipManager>();
            if (equipManager == null)
            {
                Debug.LogError("[EquipmentUI] PlayerEquipManager를 찾을 수 없습니다!", this);
            }
        }

        if (equipData == null)
        {
            equipData = FindAnyObjectByType<PlayerEquipData>();
            if (equipData == null)
            {
                Debug.LogError("[EquipmentUI] PlayerEquipData를 찾을 수 없습니다!", this);
            }
        }

        if (clothSlotObject == null)
        {
            Debug.LogError("[EquipmentUI] ClothSlot GameObject가 할당되지 않았습니다!", this);
        }

        if (shoesSlotObject == null)
        {
            Debug.LogError("[EquipmentUI] ShoesSlot GameObject가 할당되지 않았습니다!", this);
        }

        if (toolSlotObject == null)
        {
            Debug.LogError("[EquipmentUI] ToolSlot GameObject가 할당되지 않았습니다!", this);
        }

        if (slotUIPrefab == null)
        {
            Debug.LogError("[EquipmentUI] EquipmentSlotUI 프리팹이 할당되지 않았습니다!", this);
        }
    }

    private void InitializeSlotUIs()
    {
        if (slotUIPrefab == null) return;

        if (clothSlotObject != null)
        {
            clothSlotUI = CreateSlotUI(clothSlotObject.transform, GearType.Clothing);
            //Debug.Log("[EquipmentUI] ClothSlot UI 생성 완료");
        }

        if (shoesSlotObject != null)
        {
            shoesSlotUI = CreateSlotUI(shoesSlotObject.transform, GearType.Shoes);
            //Debug.Log("[EquipmentUI] ShoesSlot UI 생성 완료");
        }

        if (toolSlotObject != null)
        {
            toolSlotUI = CreateSlotUI(toolSlotObject.transform, GearType.Tool);
            //Debug.Log("[EquipmentUI] ToolSlot UI 생성 완료");
        }
    }

    private EquipmentSlotUI CreateSlotUI(Transform parent, GearType gearType)
    {
        EquipmentSlotUI newSlotUI = Instantiate(
            slotUIPrefab,
            parent.position,
            Quaternion.identity,
            parent
        );

        RectTransform rectTransform = newSlotUI.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        newSlotUI.Initialize(equipManager, gearType);

        return newSlotUI;
    }

    public void RefreshAllSlots()
    {
        RefreshClothSlot();
        RefreshShoesSlot();
        RefreshToolSLot();
    }

    public void RefreshClothSlot()
    {
        if (clothSlotUI == null || equipData == null) return;

        clothSlotUI.UpdateSlotInfo(equipData.currentClothEquip);
    }

    public void RefreshShoesSlot()
    {
        if (shoesSlotUI == null || equipData == null) return;

        shoesSlotUI.UpdateSlotInfo(equipData.currentShoesEquip);
    }

    public void RefreshToolSLot()
    {
        if (toolSlotUI == null || equipData == null) return;

        toolSlotUI.UpdateSlotInfo(equipData.currentToolEquip);
    }

    public void UpdateResistText()
    {
        if (equipManager == null) return;

        if (pollutionResText != null)
        {
            float pollutionResist = equipManager.GetTotalPollutionResist() * 100f;
            pollutionResText.text = $"{pollutionResist:F0}%";
        }
        if (heightResText != null)
        {
            float heightResist = equipManager.GetHeightResist() * 100f;
            heightResText.text = $"{heightResist:F0}%";
        }
        if (moveResText != null && playerController != null)
        {
            float moveResist = playerController.SpeedPercent;
            moveResText.text = $"{moveResist:F0}%";
        }
    }
}
