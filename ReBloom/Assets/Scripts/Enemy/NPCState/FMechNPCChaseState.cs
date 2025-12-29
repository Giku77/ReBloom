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
        if (fMechController.isPlayingJumpscare) return;

        if (fMechController == null) return;

        // 플레이어가 스테이지를 벗어났는지 체크
        if (!fMechController.IsPlayerInMyStage())
        {
            Debug.Log("[F-Mech] 리턴 스테이트 진입");
            controller.ChangeState(new FMechNPCReturnState(controller));
            return;
        }

        // 플레이어가 쳐다보고 있는지 체크
        if (fMechController.IsPlayerLookingAt())
        {
            Debug.Log("[F-Mech] 프로즌 스테이트 진입");
            controller.ChangeState(new FMechNPCFrozenState(controller));
            return;
        }

        // 플레이어 추격
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            controller.agent.SetDestination(player.position);
        }
    }

    public override void Exit()
    {
        EnemyChaseTracker.I?.OnEnemyStopChase();
    }
}