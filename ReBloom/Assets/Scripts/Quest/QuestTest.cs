using TMPro;
using UnityEngine;

public class QuestTest : MonoBehaviour
{
    private void Start()
    {
        var db = new QuestDB();
        db.LoadFromBG();
        var inventory = new DummyInventoryProvider();
        QuestManager.I.Init(db, inventory);
        var ArcR = new ArcRecipeDB();
        ArcR.LoadFromBG();
        var ArcDB = new ArcDB();
        ArcDB.LoadFromBG();
        BuildManager.I.Init(ArcDB, ArcR, inventory);
    }
}
