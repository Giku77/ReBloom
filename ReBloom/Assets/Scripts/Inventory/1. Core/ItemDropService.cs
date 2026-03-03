using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropService : MonoBehaviour
{
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private Transform player;
    [SerializeField] private float dropRadius = 2f;

    private void Awake()
    {
        if (itemSpawner == null)
        {
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
        }

        if (player == null)
        {
            var owner = GetComponentInParent<PlayerController>();
            if (owner != null)
            {
                player = owner.transform;
            }
        }
    }
    public void SetOwner(Transform owner)
    {
        player = owner;
    }
    public void DropItem(int itemID, int count)
    {
        if (player == null) return;

        Vector3 dropPos = player.position + Vector3.up * 0.5f;
        var item = ItemDatabase.I.GetItem(itemID);
        if (item != null)
        {
            itemSpawner.DropItemWithQuantity(item, dropPos, count).Forget();
            Debug.Log($"[DropItem] {item.itemName} x{count} 아이템을 드랍했습니다.");
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
