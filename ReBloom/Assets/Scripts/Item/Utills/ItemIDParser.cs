public static class ItemIDParser
{
    /// <summary>
    /// ?????? ID???? ????? ??? ????
    /// ??: 4001001 ?? ItemTableType.Consumable
    /// </summary>
    public static ItemTableType GetTableType(int itemID)
    {
        int tableNumber = itemID / 100000; // ??¡Æ ???

        if (tableNumber == 40) return ItemTableType.Consumable;
        if (tableNumber == 42) return ItemTableType.Tool;
        if (tableNumber == 43) return ItemTableType.Protective;
        if (tableNumber == 41) return ItemTableType.Misc;

        return ItemTableType.Consumable;
    }

    /// <summary>
    /// ??¬Ù? ??? ????
    /// ??: 4001001 ?? 01
    /// </summary>
    public static int GetSubCategory(int itemID)
    {
        return (itemID / 1000) % 100;
    }
}