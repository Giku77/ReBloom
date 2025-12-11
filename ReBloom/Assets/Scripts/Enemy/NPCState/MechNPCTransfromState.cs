using UnityEngine;

public class MechNPCTransformState : NPCState
{
    private MechNPCController mechController;
    private float transformDuration = 1f;
    private float stateEnterTime;

    public MechNPCTransformState(BaseNPCController controller) : base(controller)
    {
        mechController = controller as MechNPCController;
    }

    public override void Enter()
    {
        Debug.Log("[Mech] 변신 스테이트 진입!");

        controller.agent.isStopped = true;
        stateEnterTime = Time.time;

        controller.animator.ResetTrigger("Transform");
        controller.animator.SetTrigger("Transform");

        //if (controller.player != null)
        //{
        //    Vector3 direction = (controller.player.position - controller.transform.position).normalized;
        //    direction.y = 0f;
        //    controller.transform.rotation = Quaternion.LookRotation(direction);
        //}
    }

    public override void Update()
    {
        if (mechController != null && mechController.isStunned)
        {
            controller.ChangeState(new MechNPCReturnState(controller));
            return;
        }

        if (Time.time - stateEnterTime >= transformDuration)
        {
            Debug.Log("[Mech] 변신 완료! Chase 상태로 전환");
            controller.ChangeState(new MechNPCChaseState(controller));
        }
    }

    public override void Exit()
    {
        controller.animator.ResetTrigger("Transform");
        Debug.Log("[Mech] 변신 스테이트 종료");
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        float effectiveRange = controller.hearingRange * loudness;
        float distance = Vector3.Distance(controller.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            controller.lastHeardPosition = footPos;
            Debug.Log($"[Mech Transform] 변신 중 발소리 업데이트: {footPos}");
        }
    }
}