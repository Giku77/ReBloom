using UnityEngine;
using UnityEngine.AI;

public class DogFollower : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField] private Transform player;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float navMeshSearchDistance = 2f;

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;

    private NavMeshAgent agent;
    private float updateTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError("[DogFollower] NavMeshAgent가 없습니다!");
            enabled = false;
            return;
        }

        // 프리팹의 원래 회전값 저장
        Quaternion originalRotation = transform.rotation;
        Vector3 originalPosition = transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(originalPosition, out hit, navMeshSearchDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);

            // 원래 회전값 복원
            transform.rotation = originalRotation;

            if (showDebugLogs)
            {
                Debug.Log($"[DogFollower] NavMesh 배치 완료!");
            }
        }
        else
        {
            Debug.LogError($"[DogFollower] {navMeshSearchDistance}m 내에 NavMesh를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (player == null || agent == null) return;

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            agent.SetDestination(player.position);
        }
    }
    private void LateUpdate()
    {
        // 매 프레임 회전 고정
        transform.rotation = Quaternion.Euler(-90f, transform.rotation.eulerAngles.y, 0f);
    }
}