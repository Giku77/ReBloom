using UnityEngine;

public class MechNPCIdleState : NPCState
{
    private bool isRotating = false;
    private float rotationSpeed = 3f;

    public MechNPCIdleState(BaseNPCController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("NPC: Idle 상태 진입");
        controller.agent.isStopped = true;

        //controller.animator.SetTrigger("Return");
        
        float angleDifference = Quaternion.Angle(controller.transform.rotation, controller.initialRotation);
        isRotating = angleDifference > 1f;
    }

    public override void Update()
    {
        if (isRotating)
        {
            controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, controller.initialRotation, Time.deltaTime * rotationSpeed);

            if (Quaternion.Angle(controller.transform.rotation, controller.initialRotation) < 0.1f)
            {
                controller.transform.rotation = controller.initialRotation;
                isRotating = false;

                controller.animator.ResetTrigger("Return");
                controller.animator.SetTrigger("Return");
                Debug.Log("NPC: 기존 위치 이동완료");
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
            controller.ChangeState(new MechNPCTransformState(controller));
        }
    }
}