using UnityEngine;

public class DroneChaseState : NPCState
{
    private DroneNPCController drone;
    private float chaseStart;

    public DroneChaseState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        chaseStart = Time.time;
        controller.agent.isStopped = false;

        EnemyChaseTracker.I?.OnEnemyStartChase();
    }

    public override void Update()
    {
        if (drone == null) return;
        if (drone.isResting) return;
        
        if (drone.player == null)
        {
            controller.ChangeState(new DroneReturnState(controller));
            return;
        }
        
        if (drone.playerController != null && drone.playerController.isDead)
        {
            controller.ChangeState(new DroneReturnState(controller));
            return;
        }
        
        controller.agent.SetDestination(drone.player.position);
        float dist = Vector3.Distance(drone.transform.position, drone.player.position);
        
        if (dist <= drone.attackRange)
        {
            controller.ChangeState(new DroneAttackState(controller));
            return;
        }
        
        if (Time.time - chaseStart >= drone.maxChaseTime)
        {
            controller.ChangeState(new DroneReturnState(controller));
        }
    }
    public override void Exit()
    {
        EnemyChaseTracker.I?.OnEnemyStopChase();
    }
}
