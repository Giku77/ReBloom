using System;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;

    [SerializeField] private QuestPathGuide pathGuide;
    private int TargetIndex = 0;


    private void OnEnable()
    {
        //Refresh();
    }

    public void Refresh()
    {
        var qm = QuestManager.I;
        if (qm == null || qm.Current == null)
        {
            title.text = "퀘스트 없음";
            description.text = "-";
        }
        else
        {
            title.text = $"퀘스트 {qm.Current.questId}";
            description.text = qm.Current.questName;
            if (qm.Current.goals != null)
            {
                foreach (var goal in qm.Current.goals)
                {
                    if (goal.type == QuestGoalType.Collect)
                    {
                        var currentAmt = qm.Inventory.GetItemCount(goal.objectId);
                        var itemName = ItemDatabase.I.GetItem(goal.objectId)?.itemName ?? "Unknown Item";
                        description.text += $"\n - {itemName} ({currentAmt}/{goal.amount})";
                    }
                    else if (goal.type == QuestGoalType.Craft && goal.objectId != 0)
                    {
                        var currentAmt = goal.currentCount;
                        BuildManager.I.ArcDB.TryGet(goal.objectId, out var bld);
                        BuildManager.I.RecipeDB.TryGetRecipe(bld.arcId, out var recipe);
                        foreach (var (itemId, amount) in recipe.materials)
                        {
                            var itemName = ItemDatabase.I.GetItem(itemId)?.itemName ?? "Unknown Item";
                            description.text += $"\n   - {itemName} x{amount}";
                        }
                        var craftName = bld != null ? bld.name : "Unknown Building";
                        description.text += $"\n - {craftName} ({currentAmt} / {goal.amount})";
                    }
                    else if (goal.type == QuestGoalType.Enter)
                    {
                        //description.text += $"\n - 위치에 도달하기";
                        pathGuide.SetTarget(pathGuide.Target[TargetIndex], TargetIndex);
                        TargetIndex = Mathf.Clamp(TargetIndex + 1, 0, pathGuide.Target.Length - 1);
                    }
                }
            }
        }
    }

    public void ClearPathGuide()
    {
        pathGuide.ClearTarget();
    }
}
