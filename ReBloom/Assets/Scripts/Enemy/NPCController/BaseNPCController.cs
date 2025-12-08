using UnityEngine;
using UnityEngine.AI;

public abstract class BaseNPCController : MonoBehaviour
{
    [Header("Base Settings")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;
    public float hearingRange = 15f;

    protected NPCState currentState;
    public Vector3 lastHeardPosition { get; set; }
    public Vector3 initialPosition { get; protected set; }
    public Quaternion initialRotation { get; protected set; }

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        PlayerFootstep.OnFootstep += HandleFootstep;
        
        InitializeState();
    }

    protected abstract void InitializeState();

    protected virtual void Update()
    {
        currentState?.Update();
        UpdateAnimation();
    }

    protected virtual void UpdateAnimation()
    {
        if (animator != null && agent != null)
        {
            bool isMoving = !agent.isStopped && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
            float speed = isMoving ? agent.velocity.magnitude : 0f;
            animator.SetFloat("Speed", speed);
        }
    }

    public virtual void ChangeState(NPCState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void HandleFootstep(Vector3 footPos, float loudness)
    {
        currentState?.HandleFootstep(footPos, loudness);
    }

    private void OnDestroy()
    {
        PlayerFootstep.OnFootstep -= HandleFootstep;
    }
}
