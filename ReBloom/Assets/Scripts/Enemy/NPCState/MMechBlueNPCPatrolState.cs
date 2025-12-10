using Unity.VisualScripting;
using UnityEngine;

public class MMechBlueNPCPatrolState : NPCState
{
    private MMechBlueNPCController m_mech;
    private int index = -1;
    private bool waiting = false;
    private float waitTime = 2f;
    private float timer = 0f;

    public MMechBlueNPCPatrolState(BaseNPCController controller) : base(controller)
    {
        m_mech = controller as MMechBlueNPCController;
    }


    public override void Enter()
    {
        MoveNext();
    }

    private void MoveNext()
    {
        index = (index + 1) % m_mech.patrolPoints.Length;
        controller.agent.isStopped = false;
        controller.agent.SetDestination(m_mech.patrolPoints[index].position);
    }

    public override void Update()
    {
        if (!controller.agent.pathPending &&
            controller.agent.remainingDistance <= controller.agent.stoppingDistance)
        {
            MoveNext();
        }
    }
}
