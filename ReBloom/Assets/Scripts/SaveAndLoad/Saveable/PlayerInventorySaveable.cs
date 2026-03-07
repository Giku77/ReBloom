using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventorySaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private PlayerInventoryRuntime inventoryRuntime;
    [SerializeField] private NetworkObject networkObject;

    private const string PlayerInventoryGuid = "player_inventory";
    public string EntityGuid => PlayerInventoryGuid;

    private void Awake()
    {
        if (inventoryRuntime == null)
            inventoryRuntime = GetComponent<PlayerInventoryRuntime>();

        if (networkObject == null)
            networkObject = GetComponent<NetworkObject>();
    }

    private bool ShouldHandleSave()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        return networkObject != null && networkObject.IsOwner && NetworkManager.Singleton.IsServer;
    }

    public void Capture(SaveGameDTO save)
    {
        if (!ShouldHandleSave() || inventoryRuntime?.Data == null) return;

        var inventoryData = inventoryRuntime.Data;
        save.world.containers.RemoveAll(c => c.guid == PlayerInventoryGuid);

        var dto = new ContainerSaveDTO
        {
            guid = PlayerInventoryGuid,
            capacity = inventoryData.SlotCount
        };

        for (int i = 0; i < inventoryData.SlotCount; i++)
        {
            var slot = inventoryData.GetSlot(i);
            if (slot == null || slot.itemID <= 0 || slot.count <= 0)
                continue;

            dto.items.Add(new ItemSlotDTO
            {
                slot = i,
                itemId = slot.itemID,
                amount = slot.count,
            });
        }

        save.world.containers.Add(dto);
        save.player.inventoryContainerGuid = PlayerInventoryGuid;
    }

    public void Restore(SaveGameDTO save)
    {
        if (!ShouldHandleSave() || inventoryRuntime?.Data == null || save == null) return;

        var inventoryData = inventoryRuntime.Data;
        var dto = save.world.containers.FirstOrDefault(c => c.guid == PlayerInventoryGuid);
        if (dto == null) return;

        inventoryData.Clear();

        foreach (var item in dto.items)
        {
            if (item.slot < 0 || item.slot >= inventoryData.SlotCount)
                continue;

            inventoryData.SetSlotRaw(item.slot, item.itemId, item.amount);
        }

        inventoryData.NotifyChanged();
    }
}
