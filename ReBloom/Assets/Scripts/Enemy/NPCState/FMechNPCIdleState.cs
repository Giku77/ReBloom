using UnityEngine;

public class FMechNPCIdleState : NPCState
{
    private FMechNPCController fMechController;
    private bool isRotating = false;
    private float rotationSpeed = 3f;

    public FMechNPCIdleState(BaseNPCController controller) : base(controller)
    {
        fMechController = controller as FMechNPCController;
    }

    public override void Enter()
    {
        Debug.Log("F-Mech: Idle 상태 진입");
        controller.agent.isStopped = true;

        float angleDifference = Quaternion.Angle(controller.transform.rotation, controller.initialRotation);
        isRotating = angleDifference > 1f;
    }

    public override void Update()
    {
        if (isRotating)
        {
            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                controller.initialRotation,
                Time.deltaTime * rotationSpeed
            );

            if (Quaternion.Angle(controller.transform.rotation, controller.initialRotation) < 0.1f)
            {
                controller.transform.rotation = controller.initialRotation;
                isRotating = false;
            }
        }

        //if (fMechController != null && fMechController.IsPlayerInMyStage() && fMechController.IsNightTime())
        //{
        //    controller.ChangeState(new FMechNPCChaseState(controller));
        //}

        if (fMechController != null && fMechController.IsPlayerInMyStage())
        {
            controller.ChangeState(new FMechNPCChaseState(controller));
        }
    }
}