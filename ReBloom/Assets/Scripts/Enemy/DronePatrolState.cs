using UnityEngine;

public class DronePatrolState : NPCState
{
    private DroneNPCController drone;
    private int currentPatrolIndex = 0;
    private float waitTime = 2f;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    public DronePatrolState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        Debug.Log("드론: 순찰 시작");
        
        if (drone != null && drone.patrolPoints != null && drone.patrolPoints.Length > 0)
        {
            controller.agent.isStopped = false;
            MoveToNextPatrolPoint();
        }
        else
        {
            Debug.LogWarning("드론 순찰 포인트가 설정되지 않았습니다!");
        }
    }

    public override void Update()
    {
        if (drone == null || drone.patrolPoints == null || drone.patrolPoints.Length == 0)
            return;

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                MoveToNextPatrolPoint();
            }
            return;
        }

        if (!controller.agent.pathPending && controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            if (controller.agent.hasPath || controller.agent.velocity.sqrMagnitude == 0f)
            {
                controller.agent.isStopped = true;
                isWaiting = true;
                Debug.Log($"드론: 순찰 포인트 {currentPatrolIndex} 도착, 호버링 중...");
            }
        }
    }

    private void MoveToNextPatrolPoint()
    {
        if (drone == null || drone.patrolPoints == null || drone.patrolPoints.Length == 0)
            return;

        currentPatrolIndex = (currentPatrolIndex + 1) % drone.patrolPoints.Length;
        
        Transform targetPoint = drone.patrolPoints[currentPatrolIndex];
        if (targetPoint != null)
        {
            controller.agent.isStopped = false;
            controller.agent.SetDestination(targetPoint.position);
            Debug.Log($"드론: 순찰 포인트 {currentPatrolIndex}로 이동");
        }
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        float effectiveRange = controller.hearingRange * loudness;
        float distance = Vector3.Distance(controller.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            Debug.Log("드론: 발소리 감지! 추적 시작");
            controller.lastHeardPosition = footPos;
            controller.ChangeState(new DroneChaseState(controller));
        }
    }

    public override void Exit()
    {
        controller.agent.isStopped = false;
    }
}
