using UnityEngine;
using UnityEngine.AI;

public class RobotNavProxy : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float followBack = 1.2f;
    [SerializeField] private float warpDistance = 8f;

    NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = true;
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        Vector3 desired = player.position - player.forward * followBack;

        // NavMesh 위로 보정
        if (NavMesh.SamplePosition(desired, out var hit, 2.0f, NavMesh.AllAreas))
            desired = hit.position;

        agent.SetDestination(desired);

        // 너무 멀어지거나 길 못 찾으면 워프(문 너머 끼임 방지)
        float d = Vector3.Distance(transform.position, player.position);
        if (d > warpDistance || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            if (NavMesh.SamplePosition(player.position, out var hit2, 2.0f, NavMesh.AllAreas))
                agent.Warp(hit2.position);
        }
    }
}
