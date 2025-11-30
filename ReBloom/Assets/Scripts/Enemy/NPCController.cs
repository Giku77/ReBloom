using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [Header("NPC Settings")]
    public float hearingRange = 5f;
    public Transform player;

    [Header("References")]
    public NavMeshAgent agent;

    private NPCState currentState;

    public Vector3 lastHeardPosition { get; set; }
    public Vector3 initialPosition { get; private set; }
    public Quaternion initialRotation { get; private set; }
    public Animator Animator { get; private set; }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Animator = GetComponentInChildren<Animator>();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        PlayerFootstep.OnFootstep += HandleFootstep;

        ChangeState(new NPCIdleState(this));
    }

    void OnDestroy()
    {
        PlayerFootstep.OnFootstep -= HandleFootstep;
    }

    void Update()
    {
        currentState?.Update();
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