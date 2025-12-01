using UnityEngine;

public class NPCChaseState : NPCState
{
    private float attackRange = 2f;

    public NPCChaseState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("적대 NPC 플레이어 추적 시작");
        controller.agent.isStopped = false;
        controller.agent.SetDestination(controller.lastHeardPosition);
    }

    public override void Update()
    {
        if (controller.isStunned) return;

        float distanceToTarget = Vector3.Distance(controller.transform.position, controller.lastHeardPosition);
        
        if (distanceToTarget <= attackRange)
        {
            controller.ChangeState(new NPCAttackState(controller));
            return;
        }

        if (!controller.agent.pathPending && controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            if (controller.agent.hasPath || controller.agent.velocity.sqrMagnitude == 0f)
            {
                controller.ChangeState(new NPCReturnState(controller));
            }
        }
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        if (controller.isStunned) return;

        float effectiveRange = controller.hearingRange * loudness;
        float distance = Vector3.Distance(controller.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            controller.lastHeardPosition = footPos;
            controller.agent.SetDestination(controller.lastHeardPosition);
        }
    }
}