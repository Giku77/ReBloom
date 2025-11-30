using UnityEngine;

public class NPCIdleState : NPCState
{
    private bool isRotating = false;
    private float rotationSpeed = 3f;

    public NPCIdleState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("적대 NPC Idle상태전환");
        controller.agent.isStopped = true;
        
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
                Debug.Log("NPC: Rotation complete");
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