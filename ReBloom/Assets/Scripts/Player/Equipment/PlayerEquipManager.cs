using BansheeGz.BGDatabase;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEquipManager : MonoBehaviour
{
    [SerializeField] private GameObject equipInventory;
    [SerializeField] private EquipmentUI equipmentUI;
    [SerializeField] private EquipmentUI mobileequipmentUI;
    private EquipmentUI currentEquipmentUI
    {
        get
        {
            if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
                return mobileequipmentUI;
            else
                return equipmentUI;
        }
    }

    private PlayerEquipData player;
    private ToolEquipManager toolEquipManager;

    [SerializeField] private GameInventory inventoryItemData;

    public bool ExistEquipItem => player.currentShoesEquip != null || player.currentToolEquip != null || player.currentClothEquip != null;

    private PlayerAnimation anim;

    public static event Action<int> OnToolTypeChange;

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

        ItemBase defaultCloth = ItemDatabase.I.GetItem(4301001);
        ItemBase defaultShoes = ItemDatabase.I.GetItem(4302001);

        if (defaultCloth is ProtectiveItemData cloth)
        {
            Apply(cloth);
            Debug.Log($"[PlayerEquipManager] 기본 옷 장착: {cloth.itemName}");
        }

        if (defaultShoes is ProtectiveItemData shoes)
        {
            Apply(shoes);
            Debug.Log($"[PlayerEquipManager] 기본 신발 장착: {shoes.itemName}");
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

        if (currentEquipmentUI != null)
        {
            currentEquipmentUI.RefreshAllSlots();
            currentEquipmentUI.UpdateResistText();
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
            OnToolTypeChange?.Invoke((int)item.toolCategory);
        }
        else
        {
            Debug.LogError("[PlayerEquipManager] ToolEquipManager가 없습니다!");
        }

        Debug.Log($"[EquipManager] 장착 완료: {item.itemName}");

        if (currentEquipmentUI != null)
        {
            currentEquipmentUI.RefreshAllSlots();
            currentEquipmentUI.UpdateResistText();
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
               // inventory.AddItem(player.currentClothEquip.itemID, 1);
            }
            else if (protective.gearType == GearType.Shoes && player.currentShoesEquip != null)
            {
              //  inventory.AddItem(player.currentShoesEquip.itemID, 1);
            }

            Apply(protective);
            success = true;
        }
        else if (itemBase is ToolItemData tool)
        {
            // 기존 도구 해제
            if (player.currentToolEquip != null)
            {
              //  inventory.AddItem(player.currentToolEquip.itemID, 1);
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
        if (currentEquipmentUI != null)
        {
            currentEquipmentUI.RefreshAllSlots();
            currentEquipmentUI.UpdateResistText();
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

                if (!inventoryItemData.TryAddItemFromWorld(player.currentClothEquip.itemID, 1))
                {
                    Debug.Log("인벤토리 공간이 부족하여 장비를 해제할 수 없습니다.");
                    return;
                }
                player.currentClothEquip = null;
                break;

            case GearType.Shoes:
                if (!player.currentShoesEquip)
                    return;

                if (!inventoryItemData.TryAddItemFromWorld(player.currentShoesEquip.itemID, 1))
                {
                    Debug.Log("인벤토리 공간이 부족하여 장비를 해제할 수 없습니다.");
                    return;
                }
                player.currentShoesEquip = null;
                break;
                
            case GearType.Tool:
                if (!player.currentToolEquip)
                    return;

                if (!inventoryItemData.TryAddItemFromWorld(player.currentToolEquip.itemID, 1))
                {
                    Debug.Log("인벤토리 공간이 부족하여 장비를 해제할 수 없습니다.");
                    return;
                }
                player.currentToolEquip = null;
                anim.SetToolType(0);
                OnToolTypeChange?.Invoke(0);
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

        if (currentEquipmentUI != null)
        {
            currentEquipmentUI.RefreshAllSlots();
            currentEquipmentUI.UpdateResistText();
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

    #region 장착 아이템 확인, 시체박스 이전용 메서드

    /// <summary>
    /// 현재 장착 중인 아이템 ID 목록 반환
    /// </summary>
    public List<int> GetEquippedItems()
    {
        var equippedItems = new List<int>();

        if (player.currentToolEquip != null)
        {
            equippedItems.Add(player.currentToolEquip.itemID);
        }

        if (player.currentClothEquip != null)
        {
            equippedItems.Add(player.currentClothEquip.itemID);
        }

        if (player.currentShoesEquip != null)
        {
            equippedItems.Add(player.currentShoesEquip.itemID);
        }

        Debug.Log($"[PlayerEquipManager] 장착 아이템 {equippedItems.Count}개 확인");
        return equippedItems;
    }

    /// <summary>
    /// 모든 장착 데이터 초기화 (물리적 제거 포함)
    /// </summary>
    public void ClearAllEquipData()
    {
        int clearedCount = 0;

        // 도구 해제
        if (player.currentToolEquip != null)
        {
            player.currentToolEquip = null;
            anim.HandLayerChange();

            if (toolEquipManager != null)
            {
                toolEquipManager.UnequipTool();
            }
            clearedCount++;
        }

        // 옷 해제
        if (player.currentClothEquip != null)
        {
            player.currentClothEquip = null;
            clearedCount++;
        }

        // 신발 해제
        if (player.currentShoesEquip != null)
        {
            player.currentShoesEquip = null;
            clearedCount++;
        }

        Debug.Log($"[PlayerEquipManager] 장착 데이터 {clearedCount}개 초기화 완료");

        // UI 갱신
        if (currentEquipmentUI != null)
        {
            currentEquipmentUI.RefreshAllSlots();
            currentEquipmentUI.UpdateResistText();
        }

        OnToolTypeChange?.Invoke(0);
    }

    #endregion
}