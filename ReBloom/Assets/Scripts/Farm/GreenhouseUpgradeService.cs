using System.Collections.Generic;
using UnityEngine;

public static class GreenhouseUpgradeService
{
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

    public static bool Purchase(GreenhouseContext ctx, GreenhouseUpgradeState state, GreenhouseUpgradeRowData row, IItemContainer inv, bool playFeedback = true)
    {
        if (!CanPurchase(state, row, inv))
            return false;

        var removed = new List<(int itemId, int count)>();

        foreach (var (itemId, count) in row.Costs())
        {
            if (count <= 0) continue;

            if (!inv.TryRemoveItem(itemId, count))
            {
                for (int i = 0; i < removed.Count; i++)
                    inv.TryAddItem(removed[i].itemId, removed[i].count);
                return false;
            }

            removed.Add((itemId, count));
        }

        state.SetCompletedGrade(row.sort, row.grade);
        Apply(ctx, row, playFeedback);
        return true;
    }

    public static void Apply(GreenhouseContext ctx, GreenhouseUpgradeRowData row, bool playFeedback = true)
    {
        foreach (var key in row.ActiveKeys())
            ctx.TryActivate(key);

        switch (row.function)
        {
            case 1:
                if (playFeedback) SoundManager.I?.PlayBuild();
                break;
            case 2:
                if (playFeedback) SoundManager.I?.PlayBuild();
                ctx.GetComponentInChildren<GreenhouseSprinklerSystem>(true)?.gameObject.SetActive(true);
                break;
            case 3:
                break;
            case 4:
                if (playFeedback) SoundManager.I?.PlayUIClick();
                ctx.GetComponentInChildren<GreenhouseFarmDroneSystem>(true)?.gameObject.SetActive(true);
                break;
            case 5:
                if (playFeedback) SoundManager.I?.PlayUIClick();
                var drone = ctx.GetComponentInChildren<GreenhouseFarmDroneSystem>(true);
                if (drone != null)
                {
                    drone.gameObject.SetActive(true);
                    drone.SetAutoFertilize(true);
                }
                break;
        }
    }

    public static void ApplyAllSaved(GreenhouseContext ctx, GreenhouseUpgradeState state, GreenhouseUpgradeDB db, bool playFeedback = false)
    {
        if (ctx == null || state == null || db == null)
            return;

        foreach (var sort in db.GetAllSorts())
        {
            int savedGrade = state.GetCompletedGrade(sort);
            if (savedGrade <= 0) continue;

            var rows = db.GetRowsBySort(sort);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.grade <= savedGrade)
                    Apply(ctx, row, playFeedback);
            }
        }
    }
}
