public interface IInventoryProvider
{
    int GetItemCount(int itemId);
    void AddItem(int itemId, int amount);
    void RemoveItem(int itemId, int amount);
    void Clear();
    bool HasItem(int itemId, int amount);
    void Consume(int itemId, int amount);
}
