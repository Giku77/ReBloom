using UnityEngine;

public class DroneNPCController : BaseNPCController
{
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

        if (currentState is DronePatrolState || currentState is DroneChaseState)
            CheckVision();
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
        //if (Time.time - lastAttackTime < attackCooldown) return;

        animator.SetTrigger("Shoot1");

        var stats = player.GetComponent<PlayerStats>();
        if (stats != null)
            stats.TakeDamage(attackDamage);

        lastAttackTime = Time.time;

        laser.FireLaser(laserOrigin.position, laserOrigin.forward, laserLength);

        sound.PlayLaser();

        Debug.Log("드론 레이저 공격!");
    }

    public void StartRest()
    {
        isResting = true;
        ChangeState(new DroneRestState(this));
    }

    private void OnEnable()
    {
        AMechNPCController.OnPlayerDetected += HandleReinforce;
    }

    private void OnDisable()
    {
        AMechNPCController.OnPlayerDetected -= HandleReinforce;
    }

    private void HandleReinforce()
    {
        ChangeState(new DroneChaseState(this));
    }
}
