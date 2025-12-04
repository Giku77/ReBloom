using BansheeGz.BGDatabase;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEquipManager : MonoBehaviour
{
    [SerializeField] private GameObject equipInventory;
    [SerializeField] private EquipmentUI equipmentUI;

    private PlayerEquipData player;
    private ToolEquipManager toolEquipManager;

    [SerializeField] private GameInventory inventoryItemData;

    private PlayerAnimation anim;

    private void Awake()
    {
        player = GetComponent<PlayerEquipData>();
        toolEquipManager = GetComponent<ToolEquipManager>();
        anim = GetComponent<PlayerAnimation>();
        
        if (toolEquipManager == null)
        {
            Debug.LogWarning("[PlayerEquipManager] ToolEquipManager를 찾을 수 없습니다!");
        }
    }

    private void Start()
    {
        if (equipInventory != null)
        {
            equipInventory.SetActive(false);
        }
    }

    //private void Update()
    //{
    //    if (Keyboard.current.rKey.wasPressedThisFrame)
    //    {
    //        equipInventory.gameObject.SetActive(!equipInventory.activeSelf);
    //        HandleCursorState(equipInventory.activeSelf);
    //    }
    //}

    public void Apply(ProtectiveItemData item)
    {
        if (item == null)
        {
            Debug.Log("잘못 된 보호구 아이템입니다.");
            return;
        }

        switch (item.gearType)
        {
            case GearType.Clothing:
                if (player.currentClothEquip != null)
                    UnEquip(GearType.Clothing);

                player.currentClothEquip = item;
                break;

            case GearType.Shoes:
                if (player.currentShoesEquip != null)
                    UnEquip(GearType.Shoes);

                player.currentShoesEquip = item;
                break;
            case GearType.None:
                Debug.Log("장착 불가능한 보호구 타입입니다.");
                break;
            default:
                Debug.Log("잘못 된 보호구 아이템입니다.");
                return;
        }

        Debug.Log($"[EquipManager] 장착 완료: {item.itemName} (오염 저항: {item.GetPollutionResist()}%)");

        if (equipmentUI != null)
        {
            equipmentUI.RefreshAllSlots();
            equipmentUI.UpdateResistText();
            Debug.Log("[EquipManager] UI 갱신 호출");
        }
        else
        {
            Debug.LogWarning("[EquipManager] equipmentUI가 null입니다!");
        }
    }

    public void Apply(ToolItemData item)
    {
        if (item == null)
        {
            Debug.Log("잘못 된 도구 아이템입니다.");
            return;
        }

        UnEquip(GearType.Tool);
        player.currentToolEquip = item;
        
        // ToolEquipManager를 통해 실제 프리팹 생성
        if (toolEquipManager != null)
        {
            toolEquipManager.EquipTool(item);
            anim.EquipToolLayerChange();

            anim.SetToolType((int)item.toolCategory);
        }
        else
        {
            Debug.LogError("[PlayerEquipManager] ToolEquipManager가 없습니다!");
        }

        Debug.Log($"[EquipManager] 장착 완료: {item.itemName}");

        if (equipmentUI != null)
        {
            equipmentUI.RefreshAllSlots();
            equipmentUI.UpdateResistText();
            Debug.Log("[EquipManager] UI 갱신 호출");
        }
        else
        {
           //Debug.LogWarning("[EquipManager] equipmentUI가 null입니다!");
        }
    }
    public bool ToggleEquip(int itemId)
    {
        ItemBase itemBase = ItemDatabase.I.GetItem(itemId);
        if (itemBase == null) return false;

        // 이미 장착중인지 확인
        bool isEquipped = IsItemEquipped(itemId);

        if (isEquipped)
        {
            // 장착 해제
            if (itemBase is ToolItemData tool)
                UnEquip(GearType.Tool);
            else if (itemBase is ProtectiveItemData protective)
                UnEquip(protective.gearType);
            return true;
        }
        else
        {
            // 새로 장착
            return EquipItem(itemId);
        }
    }
   
    public bool EquipItem(int itemId)
    {
        ItemBase itemBase = ItemDatabase.I.GetItem(itemId);
        if (itemBase == null) return false;

        bool success = false;

        if (itemBase is ProtectiveItemData protective)
        {
            // 같은 타입 기존 장비 해제
            if (protective.gearType == GearType.Clothing && player.currentClothEquip != null)
            {
               // inventoryItemData.AddItem(player.currentClothEquip.itemID, 1);
            }
            else if (protective.gearType == GearType.Shoes && player.currentShoesEquip != null)
            {
              //  inventoryItemData.AddItem(player.currentShoesEquip.itemID, 1);
            }

            Apply(protective);
            success = true;
        }
        else if (itemBase is ToolItemData tool)
        {
            // 기존 도구 해제
            if (player.currentToolEquip != null)
            {
              //  inventoryItemData.AddItem(player.currentToolEquip.itemID, 1);
            }

            Apply(tool);
            success = true;
        }

        // 성공 시 인벤토리에서 제거
        if (success && inventoryItemData != null)
        {
            inventoryItemData.RemoveItem(itemId, 1);
        }

        // UI 갱신
        if (equipmentUI != null)
        {
            equipmentUI.RefreshAllSlots();
            equipmentUI.UpdateResistText();
        }

        return success;
    }

    public void UnEquip(GearType gearType)
    {
        switch (gearType)
        {
            case GearType.Clothing:
                if (!player.currentClothEquip)
                    return;

                inventoryItemData.AddItem(player.currentClothEquip.itemID, 1);
                player.currentClothEquip = null;
                break;

            case GearType.Shoes:
                if (!player.currentShoesEquip)
                    return;

                inventoryItemData.AddItem(player.currentShoesEquip.itemID, 1);
                player.currentShoesEquip = null;
                break;
                
            case GearType.Tool:
                if (!player.currentToolEquip)
                    return;

                inventoryItemData.AddItem(player.currentToolEquip.itemID, 1);
                player.currentToolEquip = null;
                anim.HandLayerChange();
                
                // ToolEquipManager를 통해 프리팹 제거
                if (toolEquipManager != null)
                {
                    toolEquipManager.UnequipTool();
                }
                break;

            case GearType.None:
                Debug.Log("잘못 된 보호구 타입입니다.");
                break;
        }

        // Debug.Log($"아이템 해제 완료");

        if (equipmentUI != null)
        {
            equipmentUI.RefreshAllSlots();
            equipmentUI.UpdateResistText();
        }
    }

    // 장착 여부 확인 헬퍼 메서드
    private bool IsItemEquipped(int itemId)
    {
        if (player.currentToolEquip?.itemID == itemId) return true;
        if (player.currentClothEquip?.itemID == itemId) return true;
        if (player.currentShoesEquip?.itemID == itemId) return true;
        return false;
    }

    public float GetTotalPollutionResist()
    {
        float resist = 0f;
        float clothResist = 0f;
        float shoesResist = 0f;
        if (player.currentClothEquip is ProtectiveItemData cloth)
        {
            clothResist = cloth.GetPollutionResist();
            resist += clothResist;
        }
        if (player.currentShoesEquip is ProtectiveItemData shoes)
        {
            shoesResist = shoes.GetPollutionResist();
            resist += shoesResist;
        }
        
        float finalResist = Mathf.Clamp01(resist);
        
        return finalResist;
    }

    public float GetTotalInsulationResist()
    {
        float resist = 0f;
        float clothResist = 0f;
        float shoesResist = 0f;
        if (player.currentClothEquip is ProtectiveItemData cloth)
        {
            clothResist = cloth.GetInsulationResist();
            resist += clothResist;
        }
        if (player.currentShoesEquip is ProtectiveItemData shoes)
        {
            shoesResist = shoes.GetInsulationResist();
            resist += shoesResist;
        }

        return resist;
    }

    public float GetHeightResist()
    {
        if (player.currentShoesEquip == null)
            return 1f;

        float resist = 0f;
        if (player.currentShoesEquip is ProtectiveItemData shoes)
        {
            float shoesResist = shoes.GetHeightResist();
            resist += shoesResist;
            resist = 1 - resist;
        }

        return resist;
    }

    private void HandleCursorState(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void OnCloseButtonClicked()
    {
        equipInventory.gameObject.SetActive(false);
        HandleCursorState(equipInventory.activeSelf);
    }

    public float GetToolPerform()
    {
        float perform = 0f;

        if (player.currentToolEquip is ToolItemData tool)
        {
            perform = tool.GetToolPerform();
        }

        return  1f - perform;
    }
}