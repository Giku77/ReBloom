using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [Header("Equipment Manager")]
    [SerializeField] private PlayerEquipManager equipManager;
    [SerializeField] private PlayerEquipData equipData;

    [Header("Slot UI References")]
    [SerializeField] private GameObject clothSlotObject;
    [SerializeField] private GameObject shoesSlotObject;

    [Header("Slot UI Prefab")]
    [SerializeField] private EquipmentSlotUI slotUIPrefab;

    private EquipmentSlotUI clothSlotUI;
    private EquipmentSlotUI shoesSlotUI;

private void Awake()
    {
        ValidateReferences();
    }

private void Start()
    {
        // 장비 슬롯 UI 생성
        InitializeSlotUIs();
        
        // 초기 장비 상태 표시
        RefreshAllSlots();


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

        if (slotUIPrefab == null)
        {
            Debug.LogError("[EquipmentUI] EquipmentSlotUI 프리팹이 할당되지 않았습니다!", this);
        }
    }

private void InitializeSlotUIs()
    {
        if (slotUIPrefab == null) return;

        // ClothSlot UI 생성
        if (clothSlotObject != null)
        {
            clothSlotUI = CreateSlotUI(clothSlotObject.transform, ProtectiveGearType.Clothing);
            Debug.Log("[EquipmentUI] ClothSlot UI 생성 완료");
        }

        // ShoesSlot UI 생성
        if (shoesSlotObject != null)
        {
            shoesSlotUI = CreateSlotUI(shoesSlotObject.transform, ProtectiveGearType.Shoes);
            Debug.Log("[EquipmentUI] ShoesSlot UI 생성 완료");
        }
    }

private EquipmentSlotUI CreateSlotUI(Transform parent, ProtectiveGearType gearType)
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

        // Initialize 호출
        newSlotUI.Initialize(equipManager, gearType);

        return newSlotUI;
    }

    public void RefreshAllSlots()
    {
        RefreshClothSlot();
        RefreshShoesSlot();
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



    /// <summary>
    /// 장비 해제 (외부에서 호출)
    /// </summary>



}
