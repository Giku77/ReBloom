using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropService : MonoBehaviour
{
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private Transform player;
    [SerializeField] private float dropRadius = 2f;
    public void DropItem(int itemID, int count)
    {
        if (player == null) return;

        Vector3 dropPos = player.position + Vector3.up * 0.5f;
        var item = ItemDatabase.I.GetItem(itemID);
        if (item != null)
        {
            itemSpawner.DropItemWithQuantity(item, dropPos, count).Forget();
        }
    }

    public void DropAllItems(List<ItemSlotData> items)
    {
        foreach (var item in items)
        {
            DropItem(item.itemID, item.count);
        }
    }
}
