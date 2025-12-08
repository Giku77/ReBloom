using UnityEngine;

public class NPCAttackState : NPCState
{
    private float attackRange = 2f;
    private float attackCooldown = 5f;
    private float attackDuration = 7f;
    private float stateEnterTime;

    public NPCAttackState(BaseNPCController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("NPC: 공격 스테이트 진입");

        //controller.Animator.ResetTrigger("Attack");

        controller.agent.isStopped = true;
        //stateEnterTime = Time.time;

        controller.animator.ResetTrigger("Attack");

        controller.animator.SetTrigger("Attack");
    }

    public override void Update()
    {
        //if (Time.time - stateEnterTime > attackDuration)
        //{
        //    controller.ChangeState(new NPCReturnState(controller));
        //    return;
        //}

        //if (Time.time - controller.lastAttackTime >= attackCooldown)
        //{
        //    PerformAttack();
        //    controller.lastAttackTime = Time.time;

        //    return;
        //}
    }

    private void PerformAttack()
    {
        Debug.Log("NPC: 공격 애니메이션 트리거");
        controller.animator.SetTrigger("Attack");
    }

    public override void Exit()
    {
        controller.animator.ResetTrigger("Attack");
    }

    //public override void HandleFootstep(Vector3 footPos, float loudness)
    //{
    //    float effectiveRange = controller.hearingRange * loudness;
    //    float distance = Vector3.Distance(controller.transform.position, footPos);

    //    if (distance <= effectiveRange)
    //    {
    //        if (distance <= attackRange)
    //        {
    //            stateEnterTime = Time.time;
    //        }
    //        else
    //        {
    //            controller.lastHeardPosition = footPos;
    //            controller.ChangeState(new NPCChaseState(controller));
    //        }
    //    }
    //}
}