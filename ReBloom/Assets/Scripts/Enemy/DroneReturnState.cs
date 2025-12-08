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
        Debug.Log("드론: 복귀 시작");
        drone.agent.isStopped = false;
        drone.agent.SetDestination(drone.initialPosition);
    }

    public override void Update()
    {
        if (!drone.agent.pathPending && drone.agent.remainingDistance <= drone.agent.stoppingDistance)
        {
            if (drone.agent.hasPath || drone.agent.velocity.sqrMagnitude == 0f)
            {
                if (drone.usePatrol && drone.patrolPoints != null && drone.patrolPoints.Length > 0)
                {
                    drone.ChangeState(new DronePatrolState(drone));
                }
                else
                {
                    drone.ChangeState(new DroneIdleState(drone));
                }
            }
        }
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
    }
}
