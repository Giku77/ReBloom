using UnityEngine;

public class NPCAttackState : NPCState
{
    private float attackRange = 2f;
    private float attackCooldown = 2f; // 공격 쿨다운 (애니메이션 길이보다 길게)
    private float lastAttackTime = -999f;
    private float attackDuration = 5f; // 공격 상태 유지 시간 (발소리 없으면 복귀)
    private float stateEnterTime;

    public NPCAttackState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("NPC: Entered Attack State");
        controller.agent.isStopped = true;
        stateEnterTime = Time.time;
    }

    public override void Update()
    {
        // 일정 시간 발소리 없으면 복귀
        if (Time.time - stateEnterTime > attackDuration)
        {
            controller.ChangeState(new NPCReturnState(controller));
            return;
        }

        // 공격 쿨다운 체크 후 애니메이션 트리거
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    private void PerformAttack()
    {
        Debug.Log("NPC: 공격 애니메이션 트리거");
        controller.Animator.SetTrigger("Attack");
    }

    public override void HandleFootstep(Vector3 footPos, float loudness)
    {
        float effectiveRange = controller.hearingRange * loudness;
        float distance = Vector3.Distance(controller.transform.position, footPos);

        if (distance <= effectiveRange)
        {
            // 공격 범위 내 발소리면 공격 지속, 멀면 Chase로
            if (distance <= attackRange)
            {
                stateEnterTime = Time.time; // 공격 시간 연장
            }
            else
            {
                controller.lastHeardPosition = footPos;
                controller.ChangeState(new NPCChaseState(controller));
            }
        }
    }
}