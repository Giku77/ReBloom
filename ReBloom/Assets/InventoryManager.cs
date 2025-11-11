using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    System.Action<int, GameObject> OnCreateItem;

    private void Start()
    {
        OnCreateItem += OutoftheInventory;
    }

    private void OutoftheInventory(int count, GameObject item)
    {
        // Logic for handling the item outside the inventory
    }
}
