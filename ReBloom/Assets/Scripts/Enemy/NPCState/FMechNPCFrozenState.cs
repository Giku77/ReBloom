using UnityEngine;

public class FMechNPCFrozenState : NPCState
{
    private FMechNPCController fMechController;

    public FMechNPCFrozenState(BaseNPCController controller) : base(controller)
    {
        fMechController = controller as FMechNPCController;
    }

    public override void Enter()
    {
        Debug.Log("F-Mech: Frozen 상태 (플레이어가 보고 있음)");
        controller.agent.isStopped = true;
    }

    public override void Update()
    {
        if (fMechController == null) return;

        if (!fMechController.IsPlayerInMyStage())
        {
            controller.ChangeState(new FMechNPCReturnState(controller));
            return;
        }

        if (!fMechController.IsPlayerLookingAt())
        {
            controller.ChangeState(new FMechNPCChaseState(controller));
            return;
        }
    }
}