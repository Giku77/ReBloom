using System.Linq;
using UnityEngine;

public class PlayerInventorySaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private InventoryItemData inventoryData;

    private const string PlayerInventoryGuid = "player_inventory";
    public string EntityGuid => PlayerInventoryGuid;

    public void Capture(SaveGameDTO save)
    {
        if (inventoryData == null) return;

        // 기존 동일 GUID 컨테이너 제거
        save.world.containers.RemoveAll(c => c.guid == PlayerInventoryGuid);

        var dto = new ContainerSaveDTO
        {
            guid = PlayerInventoryGuid,
            capacity = inventoryData.SlotCount
        };

        for (int i = 0; i < inventoryData.SlotCount; i++)
        {
            var s = inventoryData.GetSlot(i);
            if (s == null || s.itemID <= 0 || s.count <= 0) 
                continue;

            dto.items.Add(new ItemSlotDTO
            {
                slot = i,
                itemId = s.itemID,
                amount = s.count,
            });
        }

        save.world.containers.Add(dto);
        save.player.inventoryContainerGuid = PlayerInventoryGuid;
    }

    public void Restore(SaveGameDTO save)
    {
        if (inventoryData == null) return;

        var dto = save.world.containers.FirstOrDefault(c => c.guid == PlayerInventoryGuid);
        if (dto == null) return;

        // 기존 인벤 초기화
        inventoryData.Clear();

        // 저장된 아이템 주입
        foreach (var it in dto.items)
        {
            // 안전장치: SlotCount 바뀐 경우 대비
            if (it.slot < 0 || it.slot >= inventoryData.SlotCount)
                continue;

            // InventoryItemData에 추가한 메서드 사용
            inventoryData.SetSlotRaw(it.slot, it.itemId, it.amount);
        }

        // UI 갱신
        inventoryData.NotifyChanged();
    }
}
