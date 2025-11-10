using System.Collections.Generic;

public class DummyInventoryProvider : IInventoryProvider
{
    private Dictionary<int, int> _items = new Dictionary<int, int>()
    {
        { 2002001, 15 }, 
        { 2002002, 6 },
        { 2002005, 10 },
    };

    public int GetItemCount(int itemId)
    {
        return _items.TryGetValue(itemId, out var cnt) ? cnt : 0;
    }

    public void AddItem(int itemId, int amount)
    {
        if (_items.ContainsKey(itemId)) _items[itemId] += amount;
        else _items[itemId] = amount;
    }
}
