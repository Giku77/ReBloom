using UnityEngine;
using UnityEngine.AI;

public class MechNPCController : BaseNPCController
{
    [Header("Mech Specific")]
    public bool isStunned = false;
    public bool isJammed = false;
    public float lastAttackTime = -999f;

    private float stunEndTime = 0f;

    protected override void InitializeState()
    {
        ChangeState(new NPCIdleState(this));
    }

    protected override void Update()
    {
        base.Update();

        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;
            agent.isStopped = false;
            ChangeState(new NPCReturnState(this));
        }
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
