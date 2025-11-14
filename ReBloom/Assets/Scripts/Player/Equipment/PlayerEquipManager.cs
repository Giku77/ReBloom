using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEquipManager : MonoBehaviour
{
    private PlayerEquipData player;
    

    [SerializeField] private InventoryItemData inventoryItemData;

    private void Awake()
    {
        player = GetComponent<PlayerEquipData>();


    }

private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            
        }
    }

    public void Apply(ProtectiveItemData item)
    {
        if (item == null)
        {
            Debug.Log("잘못 된 보호구 아이템입니다.");
            return;
        }

        inventoryItemData.RemoveItem(item.itemID, 1);

        switch (item.gearType)
        {
            case ProtectiveGearType.Clothing:
                if (player.currentClothEquip != null)
                    UnEquip(ProtectiveGearType.Clothing);

                player.currentClothEquip = item;
                break;

            case ProtectiveGearType.Shoes:
                if (player.currentShoesEquip != null)
                    UnEquip(ProtectiveGearType.Shoes);

                player.currentShoesEquip = item;
                break;
            case ProtectiveGearType.None:
                Debug.Log("장착 불가능한 보호구 타입입니다.");
                break;
            default:
                Debug.Log("잘못 된 보호구 아이템입니다.");
                return;
        }


        Debug.Log($"아이템 장착 완료 {item.itemName}");
    }

    public void UnEquip(ProtectiveGearType gearType)
    {
        switch (gearType)
        {
            case ProtectiveGearType.Clothing:
                if (!player.currentClothEquip)
                    return;

                inventoryItemData.AddItem(player.currentClothEquip.itemID, 1);
                player.currentClothEquip = null;
                break;

            case ProtectiveGearType.Shoes:
                if (!player.currentShoesEquip)
                    return;

                inventoryItemData.AddItem(player.currentShoesEquip.itemID, 1);
                player.currentShoesEquip = null;
                break;
            case ProtectiveGearType.None:
                Debug.Log("잘못 된 보호구 타입입니다.");
                break;
        }

        Debug.Log($"아이템 해제 완료");
    }
}
