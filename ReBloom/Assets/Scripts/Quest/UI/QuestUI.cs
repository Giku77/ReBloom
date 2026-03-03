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

        while (_alive && NetworkQuestManager.I == null)
            await Cysharp.Threading.Tasks.UniTask.Yield();

        if (!_alive) return;

        NetworkQuestManager.I.OnQuestUpdated += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        _alive = false;

        if (NetworkQuestManager.I != null)
            NetworkQuestManager.I.OnQuestUpdated -= Refresh;
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
        var nqm = NetworkQuestManager.I;
        if (nqm == null)
            return;

        var currentQuest = nqm.GetCurrentQuest();
        if (currentQuest == null)
        {
            description.text = "-";
            return;
        }

        var db = nqm.QuestDB;
        if (db == null)
        {
            description.text = "-";
            return;
        }

        title.text = db.GetTextKR(currentQuest.questNameID);
        description.text = db.GetTextKR(currentQuest.questTextID);

        if (currentQuest.goals == null) return;

        for (int i = 0; i < currentQuest.goals.Count; i++)
        {
            var goal = currentQuest.goals[i];
            if (goal == null) continue;

            int currentAmt = nqm.GetGoalCurrentCount(i);

            if (goal.type == QuestGoalType.Collect)
            {
                var itemName = ItemDatabase.I.GetItem(goal.objectId)?.itemName ?? "Unknown Item";
                description.text += $"\n - {itemName} ({currentAmt}/{goal.amount})";
            }
            else if (goal.type == QuestGoalType.Craft)
            {
                description.text += $"\n - 제작 ({currentAmt}/{goal.amount})";
            }
            else if (goal.type == QuestGoalType.Interact)
            {
                description.text += $"\n - 상호작용 ({currentAmt}/{goal.amount})";
            }
        }
        Debug.Log($"[QuestUI] Refresh currentQuestId={nqm.CurrentQuestId} quest={(currentQuest != null)} goals={(currentQuest?.goals?.Count ?? -1)}");
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
            case (int)EntranceType.ElectricSubstation:
                return 3;
            default:
                return -1;
        }
    }
}
