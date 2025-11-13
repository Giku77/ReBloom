using TMPro;
using UnityEngine;

public class QuestTest : MonoBehaviour
{
    private void Start()
    {
        var db = new QuestDB();
        db.LoadFromBG();
        var inventory = new DummyInventoryProvider();
        var stageDetector = GameObject.FindGameObjectWithTag("Player").GetComponent<StageDetector>();
        QuestManager.I.Init(db, inventory, stageDetector);
        var ArcR = new ArcRecipeDB();
        ArcR.LoadFromBG();
        var ArcDB = new ArcDB();
        ArcDB.LoadFromBG();
        BuildManager.I.Init(ArcDB, ArcR, inventory);
    }
}
