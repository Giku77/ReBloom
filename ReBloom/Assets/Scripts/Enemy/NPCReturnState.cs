using UnityEngine;

public class NPCReturnState : NPCState
{
    public NPCReturnState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        controller.agent.isStopped = false;
        controller.agent.SetDestination(controller.initialPosition);
    }

    public override void Update()
    {
        if (!controller.agent.pathPending && controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            if (controller.agent.hasPath || controller.agent.velocity.sqrMagnitude == 0f)
            {
                controller.ChangeState(new NPCIdleState(controller));
            }
        }
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        float effectiveRange = controller.hearingRange * loudness;
        float distance = Vector3.Distance(controller.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            controller.lastHeardPosition = footPos;
            controller.ChangeState(new NPCChaseState(controller));
        }
    }
}