using UnityEngine;

public class DroneChaseState : NPCState
{
    private DroneNPCController drone;

    public DroneChaseState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        Debug.Log("드론: 추적 시작");
        drone.agent.isStopped = false;
        drone.agent.SetDestination(drone.lastHeardPosition);
        drone.StartChase();
    }

    public override void Update()
    {
        if (drone.IsChaseTimeout())
        {
            Debug.Log("드론: 추적 시간 초과 - 복귀");
            drone.ChangeState(new DroneReturnState(drone));
            return;
        }

        float distanceToTarget = Vector3.Distance(drone.transform.position, drone.lastHeardPosition);

        if (distanceToTarget <= drone.attackRange)
        {
            drone.ChangeState(new DroneAttackState(drone));
            return;
        }

        if (!drone.agent.pathPending && drone.agent.remainingDistance <= drone.agent.stoppingDistance)
        {
            if (drone.agent.hasPath || drone.agent.velocity.sqrMagnitude == 0f)
            {
                Debug.Log("드론: 플레이어 놓침 - 복귀");
                drone.ChangeState(new DroneReturnState(drone));
            }
        }
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        float effectiveRange = drone.hearingRange * loudness;
        float distance = Vector3.Distance(drone.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            drone.lastHeardPosition = footPos;
            drone.agent.SetDestination(drone.lastHeardPosition);
        }
    }
}
