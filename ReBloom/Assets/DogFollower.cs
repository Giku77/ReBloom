using UnityEngine;
using UnityEngine.AI;

public class DogFollower : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField] private Transform player;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float navMeshSearchDistance = 2f;

    [Header("텔포 설정")]
    [SerializeField] private float maxDistanceFromPlayer = 15f; // 이 거리 초과시 텔포 허용
    [SerializeField] private Vector3 teleportOffset = new Vector3(1f, 0f, 1f);

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;

    private NavMeshAgent agent;
    private float updateTimer;

    public Transform Player => player;
    public float MaxDistanceFromPlayer => maxDistanceFromPlayer;

    /// <summary>
    /// 현재 플레이어와의 거리
    /// </summary>
    public float DistanceToPlayer => player != null
        ? Vector3.Distance(transform.position, player.position)
        : float.MaxValue;

    /// <summary>
    /// 플레이어에게 가까이 있는지
    /// </summary>
    public bool IsNearPlayer => DistanceToPlayer <= maxDistanceFromPlayer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        FindPlayer();
    }

    private void FindPlayer()
    {
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

        InitializeOnNavMesh();
    }

    private void InitializeOnNavMesh()
    {
        Quaternion originalRotation = transform.rotation;
        Vector3 originalPosition = transform.position;

        if (NavMesh.SamplePosition(originalPosition, out NavMeshHit hit, navMeshSearchDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
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
        }
    }

    private void Update()
    {
        if (player == null || agent == null) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            agent.SetDestination(player.position);
        }
    }

    #region 텔포 기능

    /// <summary>
    /// 플레이어 근처로 텔포
    /// </summary>
    /// <returns>텔포 성공 여부</returns>
    public bool TeleportToPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("[DogFollower] 플레이어가 없어 텔포할 수 없습니다.");
            return false;
        }

        Vector3 targetPosition = player.position + player.TransformDirection(teleportOffset);
        return TeleportTo(targetPosition);
    }

    /// <summary>
    /// 특정 위치로 텔포
    /// </summary>
    /// <param name="targetPosition">목표 위치</param>
    /// <returns>텔포 성공 여부</returns>
    public bool TeleportTo(Vector3 targetPosition)
    {
        if (agent == null)
        {
            Debug.LogWarning("[DogFollower] NavMeshAgent가 없습니다.");
            return false;
        }

        // NavMesh 위의 유효한 위치 찾기
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, navMeshSearchDistance, NavMesh.AllAreas))
        {
            // NavMeshAgent가 활성화되어 있으면 Warp 사용
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }
            else
            {
                transform.position = hit.position;
            }

            if (showDebugLogs)
            {
                Debug.Log($"[DogFollower] 텔포 완료: {hit.position}");
            }
            return true;
        }
        else
        {
            // NavMesh를 찾지 못하면 직접 위치 설정 (fallback)
            transform.position = targetPosition;

            if (showDebugLogs)
            {
                Debug.LogWarning($"[DogFollower] NavMesh 없이 텔포: {targetPosition}");
            }
            return true; // 위치는 설정됨
        }
    }

    /// <summary>
    /// 플레이어와 멀리 떨어졌으면 자동 텔포
    /// </summary>
    /// <returns>텔포 발생 여부</returns>
    public bool TeleportIfTooFar()
    {
        if (!IsNearPlayer)
        {
            return TeleportToPlayer();
        }
        return false;
    }

    /// <summary>
    /// 텔포 예정 위치 계산 (실제 텔포하지 않음)
    /// </summary>
    public Vector3 GetTeleportPosition()
    {
        if (player == null) return transform.position;

        Vector3 targetPosition = player.position + player.TransformDirection(teleportOffset);

        // NavMesh 위치 보정
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, navMeshSearchDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPosition;
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("텔포 테스트 - 플레이어 근처로")]
    private void DebugTeleportToPlayer()
    {
        TeleportToPlayer();
    }
#endif
}