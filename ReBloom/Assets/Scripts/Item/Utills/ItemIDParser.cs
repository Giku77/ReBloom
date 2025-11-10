public static class ItemIDParser
{
    /// <summary>
    /// 아이템 ID에서 테이블 타입 추출
    /// 예: 4001001 → ItemTableType.Consumable
    /// </summary>
    public static ItemTableType GetTableType(int itemID)
    {
        int tableNumber = itemID / 100000; // 둘째 자리

        if (tableNumber == 40) return ItemTableType.Consumable;
        if (tableNumber == 42) return ItemTableType.Tool;
        if (tableNumber == 43) return ItemTableType.Protective;
        if (tableNumber == 20) return ItemTableType.Misc;

        return ItemTableType.Consumable;
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