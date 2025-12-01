using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [Header("NPC Settings")]
    public float hearingRange = 5f;
    public float lastAttackTime = -999f;
    public Transform player;

    [Header("References")]
    public NavMeshAgent agent;

    private NPCState currentState;

    public Vector3 lastHeardPosition { get; set; }
    public Vector3 initialPosition { get; private set; }
    public Quaternion initialRotation { get; private set; }
    public Animator Animator { get; private set; }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Animator = GetComponentInChildren<Animator>();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        PlayerFootstep.OnFootstep += HandleFootstep;

        ChangeState(new NPCIdleState(this));
    }

    private void OnDestroy()
    {
        PlayerFootstep.OnFootstep -= HandleFootstep;
    }

private void Update()
    {
        currentState?.Update();
        
        if (Animator != null && agent != null)
        {
            // NavMeshAgent가 실제로 이동 중인지 확인
            bool isMoving = !agent.isStopped && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
            float speed = isMoving ? agent.velocity.magnitude : 0f;
            
            Animator.SetFloat("Speed", speed);
        }
    }

    public void ChangeState(NPCState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void HandleFootstep(Vector3 footPos, float loudness)
    {
        currentState?.HandleFootstep(footPos, loudness);
    }
}