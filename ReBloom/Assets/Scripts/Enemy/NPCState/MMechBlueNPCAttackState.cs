using UnityEngine;

public class MMechBlueNPCAttackState : NPCState
{
    public MMechBlueNPCAttackState(BaseNPCController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        Debug.Log("NPC: 공격 스테이트 진입");

        controller.agent.isStopped = true;

        controller.animator.ResetTrigger("Attack");

        controller.animator.SetTrigger("Attack");
    }

    public override void Exit()
    {
        controller.animator.ResetTrigger("Attack");
    }


}
