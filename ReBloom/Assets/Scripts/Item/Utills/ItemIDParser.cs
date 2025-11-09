public static class ItemIDParser
{
    /// <summary>
    /// 아이템 ID에서 테이블 타입 추출
    /// 예: 4001001 → ItemTableType.Consumable
    /// </summary>
    public static ItemTableType GetTableType(int itemID)
    {
        int tableNumber = itemID / 1000000; // 첫 자리

        return tableNumber switch
        {
            1 => ItemTableType.Protective,
            4 => ItemTableType.Consumable,
            _ => ItemTableType.Misc
        };
    }

    /// <summary>
    /// 소분류 번호 추출
    /// 예: 4001001 → 01
    /// </summary>
    public static int GetSubCategory(int itemID)
    {
        return (itemID / 1000) % 100;
    }
}