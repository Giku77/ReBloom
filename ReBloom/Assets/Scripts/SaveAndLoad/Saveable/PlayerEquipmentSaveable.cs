using UnityEngine;

public class PlayerEquipmentSaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private PlayerEquipManager equipManager;
    [SerializeField] private PlayerEquipData equipData; // currentClothEquip / currentShoesEquip / currentToolEquip 들고있는 컴포넌트

    public string EntityGuid => "player_equipment";

    private void Awake()
    {
        if (equipManager == null) equipManager = GetComponent<PlayerEquipManager>();
        if (equipData == null) equipData = GetComponent<PlayerEquipData>();
    }

    public void Capture(SaveGameDTO save)
    {
        if (save == null || equipData == null) return;

        save.player.equipment.clothItemId = equipData.currentClothEquip ? equipData.currentClothEquip.itemID : 0;
        save.player.equipment.shoesItemId = equipData.currentShoesEquip ? equipData.currentShoesEquip.itemID : 0;
        save.player.equipment.toolItemId = equipData.currentToolEquip ? equipData.currentToolEquip.itemID : 0;
    }

    public void Restore(SaveGameDTO save)
    {
        if (save == null || equipManager == null) return;

        var dto = save.player.equipment;
        if (dto == null) return;

        equipManager.ClearAllEquipData();

        if (dto.clothItemId > 0)
        {
            var item = ItemDatabase.I?.GetItem(dto.clothItemId) as ProtectiveItemData;
            if (item != null) equipManager.Apply(item);
        }

        if (dto.shoesItemId > 0)
        {
            var item = ItemDatabase.I?.GetItem(dto.shoesItemId) as ProtectiveItemData;
            if (item != null) equipManager.Apply(item);
        }

        if (dto.toolItemId > 0)
        {
            var item = ItemDatabase.I?.GetItem(dto.toolItemId) as ToolItemData;
            if (item != null) equipManager.Apply(item);
        }
    }
}
