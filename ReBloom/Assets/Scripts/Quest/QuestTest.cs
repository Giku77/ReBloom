using TMPro;
using UnityEngine;

public class QuestTest : MonoBehaviour
{
    private void Start()
    {
        var db = new QuestDB();
        db.LoadFromBG();
        var inventory = Object.FindFirstObjectByType<GameInventory>();

        if (inventory == null)
        {
            Debug.LogError("[QuestTest] 씬에 GameInventory가 없습니다!");
            return;
        }
        QuestManager.I.Init(db, inventory);
        var ArcR = new ArcRecipeDB();
        ArcR.LoadFromBG();
        var ArcDB = new ArcDB();
        ArcDB.LoadFromBG();
        BuildManager.I.Init(ArcDB, ArcR, inventory);
    }
}
