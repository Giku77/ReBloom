using UnityEngine;

public class DroneAttackState : NPCState
{
    private DroneNPCController drone;
    private float stateEnterTime;

    public DroneAttackState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        Debug.Log("드론: 공격 상태 진입");
        drone.agent.isStopped = true;
        stateEnterTime = Time.time;

        drone.PerformLaserAttack();
    }

    public override void Update()
    {
        if (Time.time - stateEnterTime >= drone.attackCooldown)
        {
            float distanceToPlayer = Vector3.Distance(drone.transform.position, drone.player.position);

            if (distanceToPlayer <= drone.attackRange)
            {
                drone.PerformLaserAttack();
                stateEnterTime = Time.time;
            }
            else if (distanceToPlayer <= drone.detectionRange)
            {
                drone.lastHeardPosition = drone.player.position;
                drone.ChangeState(new DroneChaseState(drone));
            }
            else
            {
                drone.ChangeState(new DroneReturnState(drone));
            }
        }
    }

    public override void Exit()
    {
        // 공격 종료
    }
}
