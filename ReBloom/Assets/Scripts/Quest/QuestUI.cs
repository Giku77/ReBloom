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
            title.text = "Äù½ºÆ® ¾øÀ½";
            description.text = "-";
        }
        else
        {
            title.text = $"Äù½ºÆ® {qm.Current.questId}";
            description.text = qm.Current.questName;
        }
    }
}
