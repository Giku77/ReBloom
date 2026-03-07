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
    private bool isShowPathGuide;

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
        return pathGuide != null && pathGuide.Target != null ? pathGuide.Target.Length : 0;
    }

    public void SetShowPathGuide(bool show)
    {
        isShowPathGuide = show;
        if (!show)
            pathGuide?.ClearTarget();
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
            pathGuide?.ClearTarget();
            return;
        }

        var db = nqm.QuestDB;
        if (db == null)
        {
            description.text = "-";
            pathGuide?.ClearTarget();
            return;
        }

        title.text = db.GetTextKR(currentQuest.questNameID);
        description.text = db.GetTextKR(currentQuest.questTextID);

        bool hasActiveEnterGuide = false;

        if (currentQuest.goals != null)
        {
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
                else if (goal.type == QuestGoalType.Enter)
                {
                    description.text += $"\n - 지역 진입 ({currentAmt}/{goal.amount})";

                    if (!hasActiveEnterGuide && currentAmt < goal.amount)
                        hasActiveEnterGuide = TrySetEnterGoalGuide(goal.objectId);
                }
            }
        }

        isShowPathGuide = hasActiveEnterGuide;
        if (!hasActiveEnterGuide)
            pathGuide?.ClearTarget();

        Debug.Log($"[QuestUI] Refresh currentQuestId={nqm.CurrentQuestId} quest={(currentQuest != null)} goals={(currentQuest?.goals?.Count ?? -1)}");
    }

    public void ClearPathGuide()
    {
        pathGuide?.ClearTarget();
    }

    private bool TrySetEnterGoalGuide(int entranceType)
    {
        if (pathGuide == null || pathGuide.Target == null)
            return false;

        int index = FindEntranceIndex(entranceType);
        if (index < 0 || index >= pathGuide.Target.Length)
            return false;

        var target = pathGuide.Target[index];
        if (target == null)
            return false;

        pathGuide.SetTarget(target, index);
        return true;
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
