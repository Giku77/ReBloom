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

        // PlayerController 참조 세팅
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        if (playerController != null)
            player = playerController.transform;
        else
            Debug.LogError("[NPC] 플레이어컨트롤러를 찾을 수 없습니다.");

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
