using UnityEngine;

public class MechNPCReturnState : NPCState
{
    private MechNPCController mechController;
    public MechNPCReturnState(BaseNPCController controller) : base(controller)
    {
        mechController = controller as MechNPCController;
    }

    public override void Enter()
    {
        Debug.Log("NPC: 리턴 스테이트 진입");
        controller.agent.isStopped = false;
        controller.agent.SetDestination(controller.initialPosition);
    }

    public override void Update()
    {
        //if (mechController != null && mechController.isStunned) return;

        if (!controller.agent.pathPending && controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            if (controller.agent.hasPath || controller.agent.velocity.sqrMagnitude == 0f)
            {
                //controller.animator.SetTrigger("Return");
                controller.ChangeState(new MechNPCIdleState(controller));
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
            controller.ChangeState(new MechNPCChaseState(controller));
        }
    }
}