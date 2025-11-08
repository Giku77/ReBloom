using TMPro;
using UnityEngine;

public class QuestTest : MonoBehaviour
{
    private void Start()
    {
        var db = new QuestDB();
        db.LoadFromBG();
        QuestManager.I.Init(db);
    }
}
