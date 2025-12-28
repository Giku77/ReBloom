using UnityEngine;

public class EnemyChaseTracker : MonoBehaviour
{
    private static EnemyChaseTracker instance;
    public static EnemyChaseTracker I => instance;

    private int chasingEnemyCount = 0;
    private bool hasPlayedWarning = false;

    [SerializeField] private InventoryRobotPet robotPet;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnEnemyStartChase()
    {
        chasingEnemyCount++;

        if (chasingEnemyCount == 1 && !hasPlayedWarning)
        {
            robotPet?.PlayPoppyVoiceBySituation(7);
            hasPlayedWarning = true;
            Debug.Log("[EnemyChase] 뽀삐 경고 음성 재생");
        }

        Debug.Log($"[EnemyChase] 추격 중인 적: {chasingEnemyCount}");
    }

    public void OnEnemyStopChase()
    {
        chasingEnemyCount--;

        if (chasingEnemyCount < 0)
            chasingEnemyCount = 0;

        // 모든 적이 추격 멈춤
        if (chasingEnemyCount == 0)
        {
            hasPlayedWarning = false;
            Debug.Log("[EnemyChase] 모든 적 추격 종료, 경고 리셋");
        }

        Debug.Log($"[EnemyChase] 추격 중인 적: {chasingEnemyCount}");
    }
    public int ChasingCount => chasingEnemyCount;
}
