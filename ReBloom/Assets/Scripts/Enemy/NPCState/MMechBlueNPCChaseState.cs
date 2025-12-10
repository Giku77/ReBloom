using UnityEngine;

public class MMechBlueNPCChaseState : NPCState
{
    private MMechBlueNPCController m_mech;
    private float attackRange = 3f; 


    public MMechBlueNPCChaseState(BaseNPCController controller) : base(controller)
    {
        m_mech = controller as MMechBlueNPCController;
    }

    public override void Enter()
    {
        Debug.Log("적대 NPC 플레이어 추적 시작");
        controller.agent.isStopped = false;
        controller.agent.SetDestination(controller.player.position);
    }

    public override void Update()
    {
        if (m_mech == null) return;

        if (m_mech.player == null)
        {
            controller.ChangeState(new MMechBlueNPCPatrolState(controller));
            return;
        }

        if (m_mech.playerController != null && m_mech.playerController.isDead)
        {
            controller.ChangeState(new MMechBlueNPCPatrolState(controller));
            return;
        }

        if (!m_mech.IsCheckVision())
        {
            controller.ChangeState(new MMechBlueNPCPatrolState(controller));
            return;
        }

        controller.agent.SetDestination(m_mech.player.position);
        float dist = Vector3.Distance(m_mech.transform.position, m_mech.player.position);

        if (dist <= attackRange)
        {
            controller.ChangeState(new MMechBlueNPCAttackState(controller));
            return;
        }
    }

}
