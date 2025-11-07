using UnityEngine;
using BansheeGz.BGDatabase;

public class SheetTest : MonoBehaviour
{
    void Start()
    {
        var meta = BGRepo.I.GetMeta("Quest");
        if (meta == null)
        {
            Debug.LogError("테이블을 못 찾음");
            return;
        }
        var entity = meta.GetEntity((int)QuestIds.Quest_003);
        var fieldName = entity.Get<string>("questName");
        var fieldValue = entity.Get<int>("questID");
        Debug.Log($"Name: {fieldName}, Value: {fieldValue}");

        var metaItem = BGRepo.I.GetMeta("Item_Consumable");
        if (metaItem == null)
        {
            Debug.LogError("테이블을 못 찾음");
            return;
        }

        for(int i = 0; i < 10; i++)
        {
            var entityItem = metaItem.GetEntity(i);
            var fieldNameItem = entityItem.Get<string>("ConsumeItem_Name");
            var fieldValueItem = entityItem.Get<int>("MaxCount");
            Debug.Log($"Name: {fieldNameItem}, Value: {fieldValueItem}");
        }
    }
}
