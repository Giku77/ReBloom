using UnityEngine;
using UnityEngine.AI;

public abstract class BaseNPCController : MonoBehaviour
{
    [Header("Base Settings")]
    public NavMeshAgent agent;
    public Animator animator;

    public PlayerController playerController;
    public Transform player;

    public float hearingRange = 15f;

    protected NPCState currentState;
    public Vector3 lastHeardPosition { get; set; }
    public Vector3 initialPosition { get; protected set; }
    public Quaternion initialRotation { get; protected set; }

    public bool isStunned = false;
    protected float stunEndTime = 0f;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        PlayerFootstep.OnFootstep += HandleFootstep;
        InitializeState();
    }

    protected virtual void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
    }

    protected virtual void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
    }

    private void OnLocalPlayerSpawned(GameObject playerObj)
    {
        // NPC가 로컬 플레이어를 타겟팅 하는 구조라면(싱글처럼) 일단 이렇게
        playerController = playerObj.GetComponent<PlayerController>();
        player = playerController != null ? playerController.transform : null;
    }


    protected abstract void InitializeState();

    protected virtual void Update()
    {
        currentState?.Update();
        UpdateAnimation();
    }

    protected virtual void UpdateAnimation() { }

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

    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;
        if (agent != null)
            agent.isStopped = true;
        animator.SetTrigger("Stunned");
    }
}
