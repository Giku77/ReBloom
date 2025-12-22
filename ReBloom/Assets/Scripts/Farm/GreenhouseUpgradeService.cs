using System.Collections.Generic;
using UnityEngine;

public static class GreenhouseUpgradeService
{
    // row가 "다음 단계"인지 체크
    public static bool IsUnlocked(GreenhouseUpgradeState state, GreenhouseUpgradeRowData row)
    {
        int done = state.GetCompletedGrade(row.sort);
        return row.grade == done + 1;
    }

    public static bool IsCompleted(GreenhouseUpgradeState state, GreenhouseUpgradeRowData row)
    {
        int done = state.GetCompletedGrade(row.sort);
        return row.grade <= done;
    }

    public static bool CanPurchase(GreenhouseUpgradeState state, GreenhouseUpgradeRowData row, IItemContainer inv)
    {
        if (!IsUnlocked(state, row)) return false;
        if (inv == null) return false;

        foreach (var (itemId, count) in row.Costs())
        {
            if (inv.GetItemCount(itemId) < count)
                return false;
        }
        return true;
    }

    public static bool Purchase(GreenhouseContext ctx, GreenhouseUpgradeState state, GreenhouseUpgradeRowData row, IItemContainer inv)
    {
        if (!CanPurchase(state, row, inv))
            return false;

        var removed = new List<(int itemId, int count)>();

        foreach (var (itemId, count) in row.Costs())
        {
            if (count <= 0) continue;

            if (!inv.TryRemoveItem(itemId, count))
            {
                // 롤백
                for (int i = 0; i < removed.Count; i++)
                {
                    // TryAddItem이 없으면 AddItem으로 대체
                    inv.TryAddItem(removed[i].itemId, removed[i].count);
                }
                return false;
            }

            removed.Add((itemId, count));
        }

        state.SetCompletedGrade(row.sort, row.grade);
        Apply(ctx, row);
        return true;
    }


    public static void Apply(GreenhouseContext ctx, GreenhouseUpgradeRowData row)
    {
        // 1) 오브젝트 활성화(테이블의 Active_Prefab1~3)
        foreach (var key in row.ActiveKeys())
            ctx.TryActivate(key);

        // 2) 추가 효과(옵션)
        // function / isApplyNewArc가 필요하면 여기서 확장
        switch (row.function)
        {
            case 1: // 재배 구역 추가
                // 필요하면 FarmBed에 "유효 구역 확장" 같은 로직 연결
                break;
            case 2: // 스프링클러 설치
                ctx.GetComponentInChildren<GreenhouseSprinklerSystem>(true)?.gameObject.SetActive(true);
                break;
            case 3: // 물 탱크 정화기 설치
                break;
            case 4: // 농사용 드론 설치
                ctx.GetComponentInChildren<GreenhouseFarmDroneSystem>(true)?.gameObject.SetActive(true);
                break;
            case 5: // 농사용 드론 강화
                ctx.GetComponentInChildren<GreenhouseFarmDroneSystem>(true)?.SetAutoFertilize(true);    
                break;
        }

        if (row.isApplyNewArc)
        {
            // TODO: “기능 해금/새 Arc 적용” 같은 시스템이 있으면 호출
        }
    }

    public static void ApplyAllSaved(GreenhouseContext ctx, GreenhouseUpgradeState state, GreenhouseUpgradeDB db)
    {
        // 저장된 진행도만큼 누적 적용(비용 차감 X)
        foreach (var sort in db.GetAllSorts())
        {
            int savedGrade = state.GetCompletedGrade(sort);
            if (savedGrade <= 0) continue;

            var rows = db.GetRowsBySort(sort);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.grade <= savedGrade)
                    Apply(ctx, row);
            }
        }
    }
}
