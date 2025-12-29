using UnityEngine;

public class FMechNPCReturnState : NPCState
{
    private FMechNPCController fMechController;

    public FMechNPCReturnState(BaseNPCController controller) : base(controller)
    {
        fMechController = controller as FMechNPCController;
    }

    public override void Enter()
    {
        Debug.Log("F-Mech: Return 상태 진입");

        if (fMechController != null)
        {
            controller.agent.speed = fMechController.returnSpeed;
        }

        controller.agent.isStopped = false;
        controller.agent.SetDestination(controller.initialPosition);
        controller.animator.SetTrigger("Return");
    }

    public override void Update()
    {
        if (!controller.agent.pathPending &&
            controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            if (controller.agent.hasPath || controller.agent.velocity.sqrMagnitude == 0f)
            {
                controller.ChangeState(new FMechNPCIdleState(controller));
            }
        }
    }
}