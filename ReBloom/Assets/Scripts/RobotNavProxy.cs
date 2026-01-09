using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class RobotNavProxy : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;

    [Header("Follow")]
    [SerializeField] private float followBack = 1.2f;
    [SerializeField] private float sampleRadius = 2.0f;

    [Header("Warp Rules")]
    [SerializeField] private float hardWarpDistance = 20f;   // 너무 멀면 무조건 워프
    [SerializeField] private float minWarpDistance = 6f;     // 너무 가까우면 워프 금지
    [SerializeField] private float warpCooldown = 1.0f;

    [Header("LOS Warp")]
    [SerializeField] private Transform eye;
    [SerializeField] private LayerMask visionBlockMask;      // 벽/지형/건물만!
    [SerializeField] private float loseSightDelay = 1.2f;

    [Header("Path/Stick Warp")]
    [SerializeField] private float pathFailDelay = 0.8f;
    [SerializeField] private float stuckSpeedEps = 0.05f;
    [SerializeField] private float stuckDelay = 0.9f;

    public event Action<Vector3> OnWarp;                     // 워프하면 위치 알려줌

    private NavMeshAgent agent;
    private float lastWarpTime = -999f;

    private float lastSeenTime;
    private float lastPathOkTime;
    private float stuckTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = true;

        lastSeenTime = Time.time;
        lastPathOkTime = Time.time;
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += UnbindLocalPlayer;

        TryBindFromExistingOwner();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= UnbindLocalPlayer;
    }

    public void SetPlayer(Transform t)
    {
        player = t;
        lastSeenTime = Time.time;
        lastPathOkTime = Time.time;
    }

    private void BindLocalPlayer(GameObject go)
    {
        SetPlayer(go != null ? go.transform : null);
        // Debug.Log($"[RobotNavProxy] Bound local player = {player?.name}");
    }

    private void UnbindLocalPlayer()
    {
        player = null;
        agent.ResetPath();
    }

    private void TryBindFromExistingOwner()
    {
        var nos = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var no in nos)
        {
            if (!no.IsOwner) continue;

            if (no.GetComponent<PlayerController>() != null)
            {
                SetPlayer(no.transform);
                return;
            }
        }
    }

    private void Update()
    {
        if (player == null) return;

        // 1) 목적지(플레이어 뒤)
        Vector3 desired = player.position - player.forward * followBack;

        if (NavMesh.SamplePosition(desired, out var hit, sampleRadius, NavMesh.AllAreas))
            desired = hit.position;

        agent.SetDestination(desired);

        // 2) 시야 업데이트
        if (CanSeePlayer())
            lastSeenTime = Time.time;

        // 3) 경로 상태
        bool pathComplete = agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete;
        if (pathComplete)
            lastPathOkTime = Time.time;

        // 4) 정체 감지
        if (agent.hasPath && agent.velocity.sqrMagnitude < (stuckSpeedEps * stuckSpeedEps))
            stuckTime += Time.deltaTime;
        else
            stuckTime = 0f;

        // 5) 워프 조건
        float d = Vector3.Distance(transform.position, player.position);
        bool cooldownOk = (Time.time - lastWarpTime) > warpCooldown;

        bool hardTooFar = d > hardWarpDistance;

        bool lostSightTooLong =
            (Time.time - lastSeenTime) > loseSightDelay &&
            d > minWarpDistance;

        bool pathBadTooLong =
            (Time.time - lastPathOkTime) > pathFailDelay &&
            d > minWarpDistance &&
            (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid || !agent.hasPath);

        bool stuckTooLong =
            stuckTime > stuckDelay &&
            d > minWarpDistance;

        if (cooldownOk && (hardTooFar || lostSightTooLong || pathBadTooLong || stuckTooLong))
            WarpNearPlayer();
    }

    private void WarpNearPlayer()
    {
        Vector3 warpPos = player.position - player.forward * 1.5f;

        Vector3 finalPos;
        if (NavMesh.SamplePosition(warpPos, out var hit, sampleRadius, NavMesh.AllAreas))
            finalPos = hit.position;
        else if (NavMesh.SamplePosition(player.position, out var hit2, sampleRadius, NavMesh.AllAreas))
            finalPos = hit2.position;
        else
            finalPos = transform.position;

        agent.Warp(finalPos);

        lastWarpTime = Time.time;
        stuckTime = 0f;
        lastSeenTime = Time.time;
        lastPathOkTime = Time.time;

        OnWarp?.Invoke(finalPos);
    }

    private bool CanSeePlayer()
    {
        Transform fromT = (eye != null) ? eye : transform;

        Vector3 from = fromT.position;
        Vector3 to = player.position + Vector3.up * 1.0f;
        Vector3 dir = to - from;

        float dist = dir.magnitude;
        if (dist < 0.01f) return true;

        if (Physics.Raycast(from, dir.normalized, dist, visionBlockMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }
}
