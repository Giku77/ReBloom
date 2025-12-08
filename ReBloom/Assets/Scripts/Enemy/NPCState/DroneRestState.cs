using UnityEngine;

public class DroneRestState : NPCState
{
    private DroneNPCController drone;
    private float timer = 0f;

    public DroneRestState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        controller.agent.isStopped = true;
        timer = 0f;
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= drone.restTime)
        {
            drone.isResting = false;
            controller.ChangeState(new DronePatrolState(controller));
        }
    }
}
