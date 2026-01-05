using System.Linq;
using UnityEngine;

public class ContainerSaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private WorldStorage storage;   // 또는 컨테이너 참조

    public string EntityGuid => storage != null ? storage.ContainerGuid : "";

    public void Capture(SaveGameDTO save)
    {
        if (save?.world == null || storage == null) return;
        if (string.IsNullOrEmpty(storage.ContainerGuid)) return;

        var container = storage.GetStorageData(); 
        if (container == null) return;

        save.world.containers.RemoveAll(c => c.guid == storage.ContainerGuid);

        var dto = new ContainerSaveDTO
        {
            guid = storage.ContainerGuid,
            capacity = container.SlotCount
        };

        var items = container.Items;
        for (int i = 0; i < items.Count; i++)
        {
            var s = items[i];
            if (s == null || s.itemID <= 0 || s.count <= 0) continue;

            dto.items.Add(new ItemSlotDTO
            {
                slot = -1,
                itemId = s.itemID,
                amount = s.count
            });
        }

        save.world.containers.Add(dto);
    }

    public void Restore(SaveGameDTO save)
    {
        if (storage == null) return;
        if (string.IsNullOrEmpty(storage.ContainerGuid)) return;

        var dto = save?.world?.containers?.FirstOrDefault(c => c.guid == storage.ContainerGuid);
        if (dto == null) return;

        var container = storage.GetStorageData();
        if (container == null) return;
        Debug.Log($"[ContainerSaveable.Restore] guid={storage?.ContainerGuid} obj={gameObject.name}");

        container.Clear();
        foreach (var it in dto.items)
            container.TryAddItem(it.itemId, it.amount);
    }
}
