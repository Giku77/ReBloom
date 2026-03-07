using Unity.Netcode;
using UnityEngine;

public class PlayerEquipmentSaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private PlayerEquipManager equipManager;
    [SerializeField] private PlayerEquipData equipData;
    [SerializeField] private NetworkObject networkObject;

    public string EntityGuid => "player_equipment";

    private void Awake()
    {
        if (equipManager == null) equipManager = GetComponent<PlayerEquipManager>();
        if (equipData == null) equipData = GetComponent<PlayerEquipData>();
        if (networkObject == null) networkObject = GetComponent<NetworkObject>();
    }

    private bool ShouldHandleSave()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        return networkObject != null && networkObject.IsOwner && NetworkManager.Singleton.IsServer;
    }

    public void Capture(SaveGameDTO save)
    {
        if (save == null || equipData == null || !ShouldHandleSave()) return;

        save.player.equipment.clothItemId = equipData.currentClothEquip ? equipData.currentClothEquip.itemID : 0;
        save.player.equipment.shoesItemId = equipData.currentShoesEquip ? equipData.currentShoesEquip.itemID : 0;
        save.player.equipment.toolItemId = equipData.currentToolEquip ? equipData.currentToolEquip.itemID : 0;
    }

    public void Restore(SaveGameDTO save)
    {
        if (save == null || equipManager == null || !ShouldHandleSave()) return;

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
