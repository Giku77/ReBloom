using UnityEngine;

public class MMechRedNPCController : BaseNPCController
{
    [Header("Pastrol Settings")]
    public Transform[] patrolPoints;

    private int index = -1;

    [Header("Stun Setting")]
    [SerializeField] private float stunTime = 2f;

    protected override void InitializeState()
    {
        MoveNext();
    }

    protected override void Update()
    {
        base.Update();

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            MoveNext();
        }

        UpdateAnimation();
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

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerController.ApplyStun(stunTime);
        }
    }
}
