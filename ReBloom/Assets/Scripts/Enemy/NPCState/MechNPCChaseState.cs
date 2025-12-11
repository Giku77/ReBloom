using UnityEngine;

public class MechNPCChaseState : NPCState
{
    private MechNPCController mechController;
    private float attackRange = 3f;
    private float stateEnterTime;
    private float minChaseTime = 0.5f;

    public MechNPCChaseState(BaseNPCController controller) : base(controller)
    {
        mechController = controller as MechNPCController;
    }

    public override void Enter()
    {
        Debug.Log("적대 NPC 플레이어 추적 시작");

        controller.animator.SetTrigger("Chase");
        controller.agent.isStopped = false;
        controller.agent.SetDestination(controller.lastHeardPosition);

        stateEnterTime = Time.time;
    }

    public override void Update()
    {
        if (mechController != null && mechController.isStunned) return;

        float distanceToTarget = Vector3.Distance(controller.transform.position, controller.lastHeardPosition);
        
        if (distanceToTarget <= attackRange)
        {
            controller.ChangeState(new MechNPCAttackState(controller));
            return;
        }

        if (Time.time - stateEnterTime < minChaseTime)
            return;

        if (!controller.agent.pathPending && controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            if (controller.agent.hasPath || controller.agent.velocity.sqrMagnitude == 0f)
            {
                controller.ChangeState(new MechNPCReturnState(controller));
            }
        }
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        if (mechController != null && mechController.isStunned) return;

        float effectiveRange = controller.hearingRange * loudness;
        float distance = Vector3.Distance(controller.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            controller.lastHeardPosition = footPos;
            controller.agent.SetDestination(controller.lastHeardPosition);
        }
    }
}