using UnityEngine;

public class DronePatrolState : NPCState
{
    private DroneNPCController drone;
    private int index = -1;
    private bool waiting = false;
    private float waitTime = 2f;
    private float timer = 0f;

    public DronePatrolState(BaseNPCController controller) : base(controller)
    {
        drone = controller as DroneNPCController;
    }

    public override void Enter()
    {
        MoveNext();
    }

    private void MoveNext()
    {
        index = (index + 1) % drone.patrolPoints.Length;
        controller.agent.isStopped = false;
        controller.agent.SetDestination(drone.patrolPoints[index].position);
    }

    public override void Update()
    {
        if (waiting)
        {
            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                waiting = false;
                MoveNext();
            }
            return;
        }

        if (!controller.agent.pathPending &&
            controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            waiting = true;
            timer = 0f;
        }
    }

    public override void HandleFootstep(Vector3 pos, float loud)
    {
        float range = controller.hearingRange * loud;
        float dist = Vector3.Distance(controller.transform.position, pos);

        if (dist <= range)
            controller.ChangeState(new DroneChaseState(controller));
    }
}
