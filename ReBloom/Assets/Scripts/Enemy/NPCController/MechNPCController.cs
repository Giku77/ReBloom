using UnityEngine;
using UnityEngine.AI;

public class MechNPCController : BaseNPCController
{
    [Header("Mech Specific")]
    public bool isJammed = false;
    public float lastAttackTime = -999f;

    protected override void InitializeState()
    {
        ChangeState(new MechNPCIdleState(this));
    }

    protected override void Update()
    {
        base.Update();

        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;
            agent.isStopped = false;
            ChangeState(new MechNPCReturnState(this));
        }
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

    //public void ApplyStun(float duration)
    //{
    //    isStunned = true;
    //    stunEndTime = Time.time + duration;
    //    if (agent != null)
    //        agent.isStopped = true;
    //    animator.SetTrigger("Stunned");
    //}
}
