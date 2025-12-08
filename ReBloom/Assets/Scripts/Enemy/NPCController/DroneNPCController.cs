using UnityEngine;

public class DroneNPCController : BaseNPCController
{
    [Header("Drone Specific")]
    public Transform[] patrolPoints;
    public bool usePatrol = true;

    [SerializeField] private float flyHeight = 2f;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float detectionAngle = 90f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackDamage = 35f;
    public float attackCooldown = 3f;
    public float lastAttackTime = -999f;

    [Header("Chase")]
    public float maxChaseTime = 7f;
    public float recheckCooldown = 30f;

    private float chaseStartTime = 0f;
    private float lastRecheckTime = 0f;

    protected override void Start()
    {
        base.Start();
    }

    protected override void InitializeState()
    {
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            ChangeState(new DronePatrolState(this));
        }
        else
        {
            ChangeState(new DroneIdleState(this));
        }
    }

    protected override void Update()
    {
        base.Update();

        if (currentState is DronePatrolState || currentState is DroneChaseState)
        {
            CheckVisionDetection();
        }
    }

    private void CheckVisionDetection()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= detectionAngle / 2f)
            {
                if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, detectionRange))
                {
                    if (hit.transform == player)
                    {
                        OnPlayerDetected();
                    }
                }
            }
        }
    }

    private void OnPlayerDetected()
    {
        if (currentState is DroneIdleState)
        {
            if (Time.time - lastRecheckTime >= recheckCooldown)
            {
                lastHeardPosition = player.position;
                ChangeState(new DroneChaseState(this));
                lastRecheckTime = Time.time;
            }
        }
        else if (currentState is DroneChaseState)
        {
            lastHeardPosition = player.position;
        }
    }

    public void StartChase()
    {
        chaseStartTime = Time.time;
    }

    public bool IsChaseTimeout()
    {
        return Time.time - chaseStartTime >= maxChaseTime;
    }

    public void PerformLaserAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            var playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(attackDamage);
            }

            lastAttackTime = Time.time;

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            Debug.Log($"드론 레이저 공격! 데미지: {attackDamage}");
        }
    }

    protected override void UpdateAnimation()
    {
        if (animator != null)
        {
            bool isChasing = currentState is DroneChaseState;
            animator.SetBool("IsChasing", isChasing);
        }
    }
}
