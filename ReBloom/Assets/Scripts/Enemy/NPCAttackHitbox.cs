using UnityEngine;

/// <summary>
/// NPC 공격 히트박스 - 오른손 등 공격 부위에 붙이기
/// IsTrigger 활성화된 콜라이더 필요
/// </summary>
public class NPCAttackHitbox : MonoBehaviour
{
    [Header("Attack Settings")]
    public float damage = 50f;
    
    [Header("Debug")]
    public bool showDebugLog = true;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 감지
        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        
        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
            
            if (showDebugLog)
            {
                Debug.Log($"[NPC 히트박스] 플레이어 타격! {damage} 데미지");
            }
        }
    }

    // Inspector에서 히트박스 시각화
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && col.enabled)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
}