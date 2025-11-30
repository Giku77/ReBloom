using UnityEngine;
using UnityEngine.AI;

public enum NPCStateType { Idle, Alert, Chase }

public class NPCController : MonoBehaviour
{
    public float hearingRange = 5f;
    public Transform player;
    private NavMeshAgent agent;
    private NPCStateType currentState = NPCStateType.Idle;
    private Vector3 lastHeardPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        PlayerFootstep.OnFootstep += HandleFootstep;
    }

    void OnDestroy()
    {
        PlayerFootstep.OnFootstep -= HandleFootstep;
    }

    void Update()
    {
        switch (currentState)
        {
            case NPCStateType.Idle:
                break;
            case NPCStateType.Alert:
                agent.SetDestination(lastHeardPosition);
                if (Vector3.Distance(transform.position, lastHeardPosition) < 0.5f)
                    currentState = NPCStateType.Idle;
                break;
            case NPCStateType.Chase:
                agent.SetDestination(player.position);
                break;
        }
    }

    private void HandleFootstep(Vector3 footPos)
    {
        float distance = Vector3.Distance(transform.position, footPos);
        if (distance <= hearingRange)
        {
            lastHeardPosition = footPos;
            // 가까우면 바로 추적, 멀면 Alert
            currentState = (distance < 3f) ? NPCStateType.Chase : NPCStateType.Alert;
            Debug.Log("NPC가 발소리를 감지했습니다! 상태: " + currentState);
        }
    }
}

