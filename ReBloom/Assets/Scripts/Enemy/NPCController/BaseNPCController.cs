using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkObject))]
public abstract class BaseNPCController : NetworkBehaviour
{
    [Header("Base Settings")]
    public NavMeshAgent agent;
    public Animator animator;

    public PlayerController playerController;
    public Transform player;

    public float hearingRange = 15f;
    [SerializeField] private float targetRefreshInterval = 0.2f;

    protected NPCState currentState;
    public Vector3 lastHeardPosition { get; set; }
    public Vector3 initialPosition { get; protected set; }
    public Quaternion initialRotation { get; protected set; }

    public bool isStunned = false;
    protected float stunEndTime = 0f;
    protected bool HasServerAuthority => !IsNetworkedSession || (IsSpawned && IsServer);
    protected bool IsNetworkedSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private bool isInitialized;
    private bool isSubscribedToFootsteps;
    private float nextTargetRefreshTime;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    protected virtual void Start()
    {
        TryInitializeAuthority();
    }

    protected virtual void OnEnable()
    {
        if (HasServerAuthority)
            SubscribeFootsteps();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeFootsteps();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ConfigureAuthorityComponents();
        TryInitializeAuthority();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentState = null;
        isInitialized = false;
        UnsubscribeFootsteps();
    }

    protected abstract void InitializeState();

    protected virtual void Update()
    {
        if (!HasServerAuthority)
            return;

        RefreshTargetPlayer();
        currentState?.Update();
        UpdateAnimation();
    }

    protected virtual void UpdateAnimation() { }

    protected virtual void HandleFxEvent(int fxEventId, Vector3 position, Vector3 direction, float value) { }

    public virtual void ChangeState(NPCState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public virtual void ApplyStun(float duration)
    {
        if (IsNetworkedSession && IsSpawned && !IsServer)
        {
            ApplyStunServerRpc(duration);
            return;
        }

        ApplyStunInternal(duration);
    }

    protected virtual bool IsTargetablePlayer(PlayerController candidate)
    {
        if (candidate == null || !candidate.isActiveAndEnabled)
            return false;

        if (!IsNetworkedSession)
            return !candidate.isDead;

        NetworkPlayerOwnerGate gate = candidate.GetComponent<NetworkPlayerOwnerGate>();
        if (gate != null && gate.IsDeadAuthoritative)
            return false;

        NetworkObject candidateNetObj = candidate.GetComponent<NetworkObject>();
        return candidateNetObj != null && candidateNetObj.IsSpawned;
    }

    protected virtual PlayerController FindClosestTargetPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController closest = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (PlayerController candidate in players)
        {
            if (!IsTargetablePlayer(candidate))
                continue;

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr)
                continue;

            closest = candidate;
            closestDistanceSqr = distanceSqr;
        }

        return closest;
    }

    protected virtual void RefreshTargetPlayer(bool force = false)
    {
        if (!HasServerAuthority)
            return;

        if (!force && Time.time < nextTargetRefreshTime)
            return;

        nextTargetRefreshTime = Time.time + targetRefreshInterval;

        playerController = FindClosestTargetPlayer();
        player = playerController != null ? playerController.transform : null;
    }

    protected void BroadcastFxEvent(int fxEventId)
    {
        BroadcastFxEvent(fxEventId, transform.position, transform.forward, 0f);
    }

    protected void BroadcastFxEvent(int fxEventId, Vector3 position, Vector3 direction, float value)
    {
        if (!IsNetworkedSession)
        {
            HandleFxEvent(fxEventId, position, direction, value);
            return;
        }

        if (!IsServer)
            return;

        BroadcastFxEventRpc(fxEventId, position, direction, value);
    }

    protected void BroadcastFxEventToClient(ulong clientId, int fxEventId)
    {
        BroadcastFxEventToClient(clientId, fxEventId, transform.position, transform.forward, 0f);
    }

    protected void BroadcastFxEventToClient(ulong clientId, int fxEventId, Vector3 position, Vector3 direction, float value)
    {
        if (!IsNetworkedSession)
        {
            HandleFxEvent(fxEventId, position, direction, value);
            return;
        }

        if (!IsServer)
            return;

        BroadcastFxEventToClientRpc(fxEventId, position, direction, value, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    protected bool TryGetTargetClientId(out ulong clientId)
    {
        clientId = default;

        if (playerController == null)
            return false;

        NetworkObject targetNetObj = playerController.GetComponent<NetworkObject>();
        if (targetNetObj == null || !targetNetObj.IsSpawned)
            return false;

        clientId = targetNetObj.OwnerClientId;
        return true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyStunServerRpc(float duration)
    {
        ApplyStunInternal(duration);
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastFxEventRpc(int fxEventId, Vector3 position, Vector3 direction, float value)
    {
        HandleFxEvent(fxEventId, position, direction, value);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void BroadcastFxEventToClientRpc(int fxEventId, Vector3 position, Vector3 direction, float value, RpcParams rpcParams = default)
    {
        HandleFxEvent(fxEventId, position, direction, value);
    }

    private void HandleFootstep(Vector3 footPos, float loudness)
    {
        if (!HasServerAuthority)
            return;

        currentState?.HandleFootstep(footPos, loudness);
    }

    private void OnDestroy()
    {
        UnsubscribeFootsteps();
    }

    private void ApplyStunInternal(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;
        if (agent != null && agent.enabled)
            agent.isStopped = true;
        animator?.SetTrigger("Stunned");
    }

    private void ConfigureAuthorityComponents()
    {
        if (!IsNetworkedSession || IsServer)
            return;

        if (agent != null && agent.enabled)
            agent.enabled = false;
    }

    private void TryInitializeAuthority()
    {
        if (isInitialized)
            return;

        if (IsNetworkedSession)
        {
            if (!IsSpawned || !IsServer)
                return;
        }

        SubscribeFootsteps();
        RefreshTargetPlayer(true);
        InitializeState();
        isInitialized = true;
    }

    private void SubscribeFootsteps()
    {
        if (isSubscribedToFootsteps)
            return;

        PlayerFootstep.OnFootstep += HandleFootstep;
        isSubscribedToFootsteps = true;
    }

    private void UnsubscribeFootsteps()
    {
        if (!isSubscribedToFootsteps)
            return;

        PlayerFootstep.OnFootstep -= HandleFootstep;
        isSubscribedToFootsteps = false;
    }
}

