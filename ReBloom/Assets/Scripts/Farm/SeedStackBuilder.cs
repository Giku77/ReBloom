using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SeedStackBuilder
{
    public static List<SeedStack> Build(InventoryItemData inv, FarmDB farmDB)
    {
        var dict = new Dictionary<int,int>();

        //Debug.Log($"[SeedStackBuilder] InventoryItemData Items Count: {inv.Items.Count}");
        if (farmDB == null)
        {
            Debug.LogWarning("[SeedStackBuilder] FarmDB is null!");
        }

        foreach (var slot in inv.Items)
        {
            if (slot.itemID <= 0 || slot.count <= 0) continue;

            Debug.Log($"[SeedStackBuilder] Checking itemID: {slot.itemID}");    

            if (!farmDB.IsSeed(slot.itemID)) continue;

            Debug.Log($"[SeedStackBuilder] Checking SeeditemID: {slot.itemID}");  

            dict.TryGetValue(slot.itemID, out var cur);
            dict[slot.itemID] = cur + slot.count;
        }

        // 이름순/ID순 정렬
        return dict.Select(kv =>
        {
            var item = ItemDatabase.I.GetItem(kv.Key);
            return new SeedStack(kv.Key, item?.itemName ?? kv.Key.ToString(), kv.Value, item?.icon);
        })
        .OrderBy(s => s.seedId)
        .ToList();
    }
}

public readonly struct SeedStack
{
    public readonly int seedId;
    public readonly string name;
    public readonly int count;
    public readonly Sprite icon;
    public SeedStack(int seedId, string name, int count, Sprite icon)
    { this.seedId = seedId; this.name = name; this.count = count; this.icon = icon; }
}
