using UnityEngine;

public class DroneReturnState : NPCState
{
    private DroneNPCController drone;

    public DroneReturnState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        controller.agent.isStopped = false;
        controller.agent.SetDestination(drone.initialPosition);
    }

    public override void Update()
    {
        if (!controller.agent.pathPending &&
            controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            controller.ChangeState(new DroneRestState(controller));
        }
    }
}
