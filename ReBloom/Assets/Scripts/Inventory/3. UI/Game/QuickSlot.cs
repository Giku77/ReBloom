using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using UnityEngine;

public class QuickSlot : MonoBehaviour
{
    private int slotIndex;
    private int slotCount = 7;
    private int assignedSlotCount = 0;
    private int itemQuantity = 0;

    private ItemBase[] items;

    public List<GameObject> slotsRef;
    public GameInventory inventory;
    public QuickSlotUI QuickSlotUIPrefab;
    public ReadOnlyCollection<ItemBase> GetItemBaseSlot => Array.AsReadOnly(items);

    public System.Action<ItemBase, int> OnSlotAssign;

    public bool TryAssign(ItemBase item, int quantity)
    {
        if (assignedSlotCount >= slotCount || item == null)
        {
            return false;
        }
        else
        {
            Assign(item, quantity);
            return true;
        }
    }

    private void Assign(ItemBase item, int quantity)
    {
        items[slotIndex] = item;
        itemQuantity = quantity;

        if (slotIndex < 0 || slotIndex >= slotsRef.Count)
        {
            Debug.LogError($"Slot index {slotIndex} is out of range.");
            return;
        }

        var itemslot = Instantiate(QuickSlotUIPrefab, slotsRef[slotIndex].transform.position, UnityEngine.Quaternion.identity);

        itemslot.transform.SetParent(slotsRef[slotIndex].transform);
        QuickSlotUIPrefab.OnUpdateSlotInfo(item, quantity);

        slotIndex++;
        assignedSlotCount = slotIndex;
    }

    private void Awake()
    {
        items = new ItemBase[slotCount]; // Fixed: Properly initialized the array with the specified size.
    }

    void Start()
    {
    }

    void Update()
    {
    }
}
