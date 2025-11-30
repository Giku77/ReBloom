using UnityEngine;

public class NPCAttackState : NPCState
{
    private float attackRange = 2f;
    private float attackCooldown = 5f;
    private float lastAttackTime = -999f;
    private float attackDuration = 7f;
    private float stateEnterTime;

    public NPCAttackState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("NPC: Entered Attack State");
        controller.agent.isStopped = true;
        stateEnterTime = Time.time;
    }

    public override void Update()
    {
        if (Time.time - stateEnterTime > attackDuration)
        {
            controller.ChangeState(new NPCReturnState(controller));
            return;
        }

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    private void PerformAttack()
    {
        Debug.Log("NPC: 공격 애니메이션 트리거");
        controller.Animator.SetTrigger("Attack");
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        float effectiveRange = controller.hearingRange * loudness;
        float distance = Vector3.Distance(controller.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            if (distance <= attackRange)
            {
                stateEnterTime = Time.time;
            }
            else
            {
                controller.lastHeardPosition = footPos;
                controller.ChangeState(new NPCChaseState(controller));
            }
        }
    }
}