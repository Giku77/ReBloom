using BansheeGz.BGDatabase;
using UnityEngine;

/// <summary>
/// 아이템 팩토리 - BGEntity로부터 적절한 ItemBase 생성
/// </summary>
public static class ItemFactory
{
    /// <summary>
    /// BGEntity의 테이블 이름을 보고 알맞은 아이템 생성
    /// </summary>
    public static ItemBase CreateItem(BGEntity entity, ItemTableType tableType)
    {
        if (entity == null)
        {
            Debug.LogError("[ItemFactory] Entity가 null");
            return null;
        }

        return tableType switch
        {
            ItemTableType.Consumable => CreateConsumableItem(entity),
            ItemTableType.Protective => CreateProtectiveItem(entity),
            ItemTableType.Tool => CreateToolItem(entity),
            ItemTableType.Misc => CreateMiscItem(entity),
        };
    }

    /// <summary>
    /// 소비 아이템 생성
    /// </summary>
    private static ConsumableItemData CreateConsumableItem(BGEntity entity)
    {
        var item = ScriptableObject.CreateInstance<ConsumableItemData>();
        item.Initialize(entity);
        return item;
    }

    /// <summary>
    /// 보호구 생성
    /// </summary>
    private static ProtectiveItemData CreateProtectiveItem(BGEntity entity)
    {
        var item = ScriptableObject.CreateInstance<ProtectiveItemData>();
        item.Initialize(entity);
        return item;
    }

    /// <summary>
    /// 도구 생성 (TODO)
    /// </summary>
    private static ItemBase CreateToolItem(BGEntity entity)
    {
        // var item = ScriptableObject.CreateInstance<ToolItemData>();
        // item.Initialize(entity);
        // return item;

        Debug.LogWarning("[ItemFactory] 도구 아이템은 아직 구현 X");
        return null;
    }

    /// <summary>
    /// 기타 아이템 생성 (TODO)
    /// </summary>
    private static ItemBase CreateMiscItem(BGEntity entity)
    {
        Debug.LogWarning("[ItemFactory] 기타 아이템은 아직 구현 X");
        return null;
    }
}