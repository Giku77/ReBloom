using BansheeGz.BGDatabase;
using UnityEngine;

/// <summary>
/// 아이템 팩토리 - BGEntity로부터 적절한 ItemBase 생성
/// Factory Pattern: 객체 생성 로직을 한 곳에 집중
/// </summary>
public static class ItemFactory
{
    /// <summary>
    /// BGEntity의 테이블 타입을 보고 알맞은 아이템 생성
    /// </summary>
    /// <param name="entity">BG Database Entity</param>
    /// <param name="tableType">아이템 테이블 타입</param>
    /// <returns>생성된 ItemBase 객체 (실패 시 null)</returns>
    public static ItemBase CreateItem(BGEntity entity, ItemTableType tableType)
    {
        if (entity == null)
        {
            Debug.LogError("[ItemFactory] Entity가 null입니다!");
            return null;
        }

        // 테이블 타입에 따라 적절한 아이템 클래스 생성
        return tableType switch
        {
            ItemTableType.Consumable => CreateConsumableItem(entity),
            ItemTableType.Protective => CreateProtectiveItem(entity),
            ItemTableType.Tool => CreateToolItem(entity),
            ItemTableType.Misc => CreateMiscItem(entity),
            _ => HandleUnknownType(tableType)
        };
    }

    #region 아이템 타입별 생성 메서드

    /// <summary>
    /// 소비 아이템 생성 (음식, 물, 약품 등)
    /// </summary>
    private static ConsumableItemData CreateConsumableItem(BGEntity entity)
    {
        try
        {
            var item = ScriptableObject.CreateInstance<ConsumableItemData>();
            item.Initialize(entity);

            Debug.Log($"[ItemFactory] 소비 아이템 생성: {item.itemName} (ID: {item.itemID})");
            return item;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemFactory] 소비 아이템 생성 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 보호구 생성 (방진 마스크, 방호복 등)
    /// </summary>
    private static ProtectiveItemData CreateProtectiveItem(BGEntity entity)
    {
        try
        {
            var item = ScriptableObject.CreateInstance<ProtectiveItemData>();
            item.Initialize(entity);

            Debug.Log($"[ItemFactory] 보호구 생성: {item.itemName} (ID: {item.itemID})");
            return item;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemFactory] 보호구 생성 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 도구 생성 (삽, 곡괭이, 가방 등)
    /// </summary>
    private static ToolItemData CreateToolItem(BGEntity entity)
    {
        try
        {
            var item = ScriptableObject.CreateInstance<ToolItemData>();
            item.Initialize(entity);

            Debug.Log($"[ItemFactory] 도구 생성: {item.itemName} (ID: {item.itemID})");
            return item;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemFactory] 도구 생성 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 기타 아이템 생성 (종자, 자원, 재료 등)
    /// </summary>
    private static MiscItemData CreateMiscItem(BGEntity entity)
    {
        try
        {
            var item = ScriptableObject.CreateInstance<MiscItemData>();
            item.Initialize(entity);

            Debug.Log($"[ItemFactory] 기타 아이템 생성: {item.itemName} (ID: {item.itemID})");
            return item;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemFactory] 기타 아이템 생성 실패: {e.Message}");
            return null;
        }
    }

    #endregion

    #region 에러 핸들링

    /// <summary>
    /// 알 수 없는 테이블 타입 처리
    /// </summary>
    private static ItemBase HandleUnknownType(ItemTableType tableType)
    {
        Debug.LogError($"[ItemFactory] 알 수 없는 테이블 타입: {tableType}");
        return null;
    }

    #endregion

    #region 디버그 유틸리티

    /// <summary>
    /// 아이템 생성 가능 여부 검증
    /// </summary>
    public static bool CanCreateItem(BGEntity entity, ItemTableType tableType)
    {
        if (entity == null)
        {
            Debug.LogWarning("[ItemFactory] Entity가 null입니다.");
            return false;
        }

        // 필수 필드 존재 여부 확인
        switch (tableType)
        {
            case ItemTableType.Consumable:
                return HasRequiredFields(entity, "ConsumeItem_ID", "ConsumeItem_Name");

            case ItemTableType.Protective:
                return HasRequiredFields(entity, "Equip_ID", "Equip_Name");

            case ItemTableType.Tool:
                return HasRequiredFields(entity, "Tool_ID", "Tool_Name");

            case ItemTableType.Misc:
                return HasRequiredFields(entity, "Item_ID", "Item_Name");

            default:
                return false;
        }
    }

    /// <summary>
    /// 필수 필드 존재 여부 확인
    /// </summary>
    private static bool HasRequiredFields(BGEntity entity, params string[] fieldNames)
    {
        var meta = entity.Meta;

        foreach (string fieldName in fieldNames)
        {
            if (meta.GetField(fieldName) == null)
            {
                Debug.LogWarning($"[ItemFactory] 필드를 찾을 수 없음: {fieldName}");
                return false;
            }
        }

        return true;
    }

    #endregion
}