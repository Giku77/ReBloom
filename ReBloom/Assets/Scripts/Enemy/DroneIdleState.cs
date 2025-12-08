using UnityEngine;

public class DroneIdleState : NPCState
{
    private DroneNPCController drone;

    public DroneIdleState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        Debug.Log("드론: Idle 상태 진입 (호버링)");
        drone.agent.isStopped = true;
    }

    public override void Update()
    {
        // 호버링 애니메이션 (BaseNPCController의 Update에서 처리)
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        float effectiveRange = drone.hearingRange * loudness;
        float distance = Vector3.Distance(drone.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            drone.lastHeardPosition = footPos;
            drone.ChangeState(new DroneChaseState(drone));
        }
    }
}
