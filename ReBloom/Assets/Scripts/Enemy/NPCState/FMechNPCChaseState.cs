using UnityEngine;

public class FMechNPCChaseState : NPCState
{
    private FMechNPCController fMechController;

    public FMechNPCChaseState(BaseNPCController controller) : base(controller)
    {
        fMechController = controller as FMechNPCController;
    }

    public override void Enter()
    {
        Debug.Log("F-Mech: Chase 상태 진입");

        if (fMechController != null)
        {
            controller.agent.speed = fMechController.chaseSpeed;
        }

        controller.agent.isStopped = false;
        controller.animator.SetTrigger("Chase");
    }

    public override void Update()
    {
        if (fMechController == null) return;
        if (fMechController.isPlayingJumpscare) return;

        if (!fMechController.IsPlayerInMyStage())
        {
            Debug.Log("[F-Mech] 리턴 스테이트 진입");
            controller.ChangeState(new FMechNPCReturnState(controller));
            return;
        }

        if (fMechController.IsPlayerLookingAt())
        {
            Debug.Log("[F-Mech] 프로즌 스테이트 진입");
            controller.ChangeState(new FMechNPCFrozenState(controller));
            return;
        }

        if (controller.player != null)
        {
            controller.agent.SetDestination(controller.player.position);
        }
    }

    public override void Exit()
    {
        EnemyChaseTracker.I?.OnEnemyStopChase();
    }
}
