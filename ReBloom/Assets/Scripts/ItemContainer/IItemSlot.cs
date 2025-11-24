/// <summary>
/// 모든 아이템 슬롯의 공통 인터페이스
/// </summary>
public interface IItemSlot
{
    void SetItem(ItemBase item, int quantity);
    void Clear();
    ItemBase GetItem();
}