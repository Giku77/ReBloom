using UnityEngine;

public class DroneAttackState : NPCState
{
    private DroneNPCController drone;

    private float attackDuration = 7f;
    private float durationTimer = 0f;

    private float attackInterval = 1f;
    private float attackTimer = 0f;

    public DroneAttackState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        durationTimer = 0f;
        attackTimer = 0f;

        controller.agent.isStopped = false;
    }

    public override void Update()
    {
        if (controller.player == null)
        {
            controller.ChangeState(new DroneReturnState(base.controller));
            return;
        }

        durationTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;

        if (drone.playerController != null && drone.playerController.isDead)
        {
            controller.ChangeState(new DroneReturnState(controller));
            return;
        }

        if (durationTimer >= attackDuration)
        {
            controller.ChangeState(new DroneReturnState(base.controller));
            return;
        }

        controller.agent.SetDestination(controller.player.position);

        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            drone.AttackNow();
        }
    }

    public override void Exit()
    {
        controller.agent.isStopped = true;
    }
}

