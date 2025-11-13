using System.Collections.Generic;

public class DummyInventoryProvider : IInventoryProvider
{
    private Dictionary<int, int> _items = new Dictionary<int, int>()
    {
        //{ 4102001, 15 }, 
        //{ 4102002, 6 },
        //{ 4102005, 10 },
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

    public void RemoveItem(int itemId, int amount)
    {
        if (_items.ContainsKey(itemId))
        {
            _items[itemId] -= amount;
            if (_items[itemId] < 0) _items[itemId] = 0;
        }
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool HasItem(int itemId, int amount)
    {
        return GetItemCount(itemId) >= amount;
    }

    public void Consume(int itemId, int amount)
    {
        RemoveItem(itemId, amount);
    }
}
