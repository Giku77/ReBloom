using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;

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
                    else
                    {
                        // 건축해야하는 건축물 표시
                        //description.text += $"\n - {}";
                    }
                }
            }
        }
    }
}
