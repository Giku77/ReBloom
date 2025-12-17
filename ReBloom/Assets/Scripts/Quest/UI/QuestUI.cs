using System;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;

    [SerializeField] private QuestPathGuide pathGuide;

    [SerializeField] private InventoryItemData inventoryData;


    private bool _alive;
    private async void OnEnable()
    {
        _alive = true;
        while (QuestManager.I == null) await Cysharp.Threading.Tasks.UniTask.Yield();
        if (!_alive) return;
        QuestManager.I.OnQuestStateChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        _alive = false;
        if (QuestManager.I != null)
            QuestManager.I.OnQuestStateChanged -= Refresh;
    }


    public int GetPathTransformCount()
    {
        return pathGuide.Target.Length;
    }
    //[NonSerialized] public int TargetIndex = 0;

    private bool isShowPathGuide = false;


    public void SetShowPathGuide(bool show)
    {
        isShowPathGuide = show;
    }

    public bool GetShowPathGuide()
    {
        return isShowPathGuide;
    }

    public void Refresh()
    {
        var qm = QuestManager.I;
        if (qm == null || qm.Current == null)
        {
            //title.text = "퀘스트 없음";
            description.text = "-";
        }
        else
        {
            //title.text = $"퀘스트 {qm.Current.questId}";
            title.text = qm.DB.GetTextKR(qm.Current.questNameID);
            description.text = qm.DB.GetTextKR(qm.Current.questTextID);
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
                    else if (goal.type == QuestGoalType.Enter && !isShowPathGuide)
                    {
                        //description.text += $"\n - 위치에 도달하기";
                        //pathGuide.SetTarget(pathGuide.Target[TargetIndex], TargetIndex);
                        pathGuide.SetTarget(pathGuide.Target[FindEntranceIndex(goal.objectId)], FindEntranceIndex(goal.objectId));
                        SetShowPathGuide(true);
                    }
                }
            }
        }
    }

    public void ClearPathGuide()
    {
        pathGuide.ClearTarget();
    }

    private int FindEntranceIndex(int entranceType)
    {
        switch (entranceType)
        {
            case (int)EntranceType.AbandonedSchool:
                return 0;
            case (int)EntranceType.DepartmentStore:
                return 1;
            case (int)EntranceType.Factory:
                return 2;
            default:
                return -1;
        }
    }
}
