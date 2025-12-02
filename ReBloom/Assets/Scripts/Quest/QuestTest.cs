using TMPro;
using UnityEngine;

public class QuestTest : MonoBehaviour
{

    private void Awake()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;
    }
    private void Start()
    {
        var db = new QuestDB();
        db.LoadFromBG();
        var tutorialDB = new TutorialDB();
        tutorialDB.LoadFromBG();
        var inventory = FindFirstObjectByType<GameInventory>();
        var stageDetector = GameObject.FindGameObjectWithTag("Player").GetComponent<StageDetector>();

        if (inventory == null)
        {
            Debug.LogError("[QuestTest] 씬에 GameInventory가 없습니다!");
            return;
        }
        QuestManager.I.Init(db, inventory, stageDetector);
    }
}
