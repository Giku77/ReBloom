using UnityEngine;

public class QuestTest : MonoBehaviour
{
    private bool initialized;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;

        // (선택) 이미 로컬플레이어가 떠있는 케이스 커버:
        // if (LocalPlayer.GO != null) BindLocalPlayer(LocalPlayer.GO);
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
    }

    private void BindLocalPlayer(GameObject playerObj)
    {
        if (initialized) return;
        if (playerObj == null) return;

        var inventory = FindFirstObjectByType<GameInventory>();
        if (inventory == null)
        {
            Debug.LogError("[QuestTest] 씬에 GameInventory가 없습니다!");
            return;
        }

        var stageDetector = playerObj.GetComponent<StageDetector>();
        if (stageDetector == null)
        {
            stageDetector = StageDetector.I;
        }

        var db = new QuestDB();
        db.LoadFromBG();

        var tutorialDB = new TutorialDB();
        tutorialDB.LoadFromBG(); // 지금은 사용 안 하지만 기존 코드 유지

        QuestManager.I.Init(db, inventory, stageDetector);

        initialized = true;
        Debug.Log("[QuestTest] QuestManager initialized (local player bound).");
    }
}
