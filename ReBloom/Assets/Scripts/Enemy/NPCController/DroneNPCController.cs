using UnityEngine;

public class DroneNPCController : BaseNPCController
{
    private const int FxDetection = 1;
    private const int FxLaser = 2;

    [Header("Drone Settings")]
    public Transform[] patrolPoints;
    public bool usePatrol = true;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float detectionAngle = 90f;

    [Header("Attack")]
    public float attackRange = 3f;
    public float laserLength = 3.5f;
    public float attackDamage = 35f;
    public float attackCooldown = 3f;
    public float lastAttackTime = -999f;
    [SerializeField] private Transform laserOrigin;

    [Header("Chase")]
    public float maxChaseTime = 7f;

    [Header("Rest")]
    public float restTime = 30f;
    public bool isResting = false;

    [HideInInspector] public LaserRendererHandler laser;

    public DroneNPCSound sound;

    protected override void Start()
    {
        base.Start();
        laser = GetComponent<LaserRendererHandler>();

        sound = GetComponentInChildren<DroneNPCSound>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        AMechNPCController.OnPlayerDetected += HandleReinforce;
    }

    protected override void OnDisable()
    {
        AMechNPCController.OnPlayerDetected -= HandleReinforce;
        base.OnDisable();
    }

    protected override void InitializeState()
    {
        if (usePatrol)
            ChangeState(new DronePatrolState(this));
        else
            ChangeState(new DroneRestState(this));
    }

    protected override void Update()
    {
        if (isResting) return;

        base.Update();

        if (!HasServerAuthority)
            return;

        if (currentState is DronePatrolState || currentState is DroneChaseState)
            CheckVision();
    }

    protected override void HandleFxEvent(int fxEventId, Vector3 position, Vector3 direction, float value)
    {
        switch (fxEventId)
        {
            case FxDetection:
                sound?.PlayDetection();
                break;
            case FxLaser:
                laser?.FireLaser(position, direction, value);
                sound?.PlayLaser();
                break;
        }
    }

    public void PlayDetectionFx()
    {
        BroadcastFxEvent(FxDetection);
    }

    private void CheckVision()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return;

        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > detectionAngle * 0.5f) return;

        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == player)
                OnPlayerDetected();
        }
    }

    public void OnPlayerDetected()
    {
        if (isResting) return;

        if (!(currentState is DroneChaseState))
            ChangeState(new DroneChaseState(this));
    }

    public void Attack()
    {
        animator.SetTrigger("Shoot1");

        NetworkPlayerOwnerGate gate = player != null ? player.GetComponent<NetworkPlayerOwnerGate>() : null;
        if (gate != null)
            gate.ApplyAuthoritativeDamage(attackDamage);

        lastAttackTime = Time.time;

        BroadcastFxEvent(FxLaser, laserOrigin.position, laserOrigin.forward, laserLength);

        Debug.Log("드론 레이저 공격!");
    }

    public void StartRest()
    {
        isResting = true;
        ChangeState(new DroneRestState(this));
    }

    private void HandleReinforce()
    {
        if (!HasServerAuthority)
            return;

        ChangeState(new DroneChaseState(this));
    }
}
