using UnityEngine;

public class NPCAttackBehaviour : StateMachineBehaviour
{
    [Header("Attack Timing")]
    [Range(0f, 1f)]
    public float hitboxStartTime = 0.3f;
    [Range(0f, 1f)]
    public float hitboxEndTime = 0.7f;

    [Header("Hitbox Reference")]
    public string hitboxObjectName = "RightHand";

    private bool hitboxActivated = false;
    private Collider hitboxCollider;
    private Transform npcTransform;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hitboxActivated = false;
        
        if (hitboxCollider == null)
        {
            npcTransform = animator.transform;
            Transform hitboxTransform = FindChildRecursive(npcTransform, hitboxObjectName);
            
            if (hitboxTransform != null)
            {
                hitboxCollider = hitboxTransform.GetComponent<Collider>();
                
                if (hitboxCollider != null)
                {
                    Debug.Log($"[NPC Attack] 히트박스 찾음: {hitboxObjectName}");
                }
                else
                {
                    Debug.LogWarning($"[NPC Attack] {hitboxObjectName}에 Collider가 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning($"[NPC Attack] {hitboxObjectName}을 찾을 수 없습니다!");
            }
        }
        
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
        
        Debug.Log("[NPC Attack] 공격 애니메이션 시작");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hitboxCollider == null) return;

        float normalizedTime = stateInfo.normalizedTime % 1f;
        
        if (normalizedTime >= hitboxStartTime && normalizedTime <= hitboxEndTime)
        {
            if (!hitboxActivated)
            {
                hitboxCollider.enabled = true;
                hitboxActivated = true;
                Debug.Log($"[NPC Attack] 히트박스 활성화 ({normalizedTime:F2})");
            }
        }
        else if (hitboxActivated)
        {
            hitboxCollider.enabled = false;
            hitboxActivated = false;
            Debug.Log($"[NPC Attack] 히트박스 비활성화 ({normalizedTime:F2})");
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
        
        Debug.Log("[NPC Attack] 공격 애니메이션 종료");

        MechNPCController npc = animator.GetComponentInParent<MechNPCController>();
        if (npc != null)
        {
            npc.ChangeState(new NPCReturnState(npc));
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}