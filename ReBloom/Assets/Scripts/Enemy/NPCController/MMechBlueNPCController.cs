using UnityEngine;

public class MMechBlueNPCController : BaseNPCController
{
    [Header("Pastrol Settings")]
    public Transform[] patrolPoints;

    private int index = -1;

    [Header("Stun Setting")]
    public float stunTime = 2f;

    [Header("Detection")]
    public float detectionRange = 6f;
    public float detectionAngle = 90f;

    protected override void InitializeState()
    {
        ChangeState(new MMechBlueNPCPatrolState(this));
    }

    protected override void Update()
    {
        base.Update();

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            MoveNext();
        }

        UpdateAnimation();

        if (currentState is MMechBlueNPCPatrolState || currentState is MMechBlueNPCChaseState)
            IsCheckVision();
    }

    protected override void UpdateAnimation()
    {
        if (animator != null && agent != null)
        {
            bool isMoving = !agent.isStopped && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
            float speed = isMoving ? agent.velocity.magnitude : 0f;
            animator.SetFloat("Speed", speed);
        }
    }

    private void MoveNext()
    {
        index = (index + 1) % patrolPoints.Length;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[index].position);
    }
    public bool IsCheckVision()
    {
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > detectionAngle * 0.5f) return false;

        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectionRange))
        {
            OnPlayerDetected();
            return hit.transform == player;
        }

        return false;
    }

    //private void CheckVision()
    //{
    //    if (player == null) return;

    //    float dist = Vector3.Distance(transform.position, player.position);
    //    if (dist > detectionRange) return;

    //    Vector3 dir = (player.position - transform.position).normalized;
    //    float angle = Vector3.Angle(transform.forward, dir);
    //    if (angle > detectionAngle * 0.5f) return;

    //    if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectionRange))
    //    {
    //        if (hit.transform == player)
    //            OnPlayerDetected();
    //    }
    //}

    public void OnPlayerDetected()
    {
        if (!(currentState is MMechBlueNPCChaseState))
            ChangeState(new MMechBlueNPCChaseState(this));
    }
}
