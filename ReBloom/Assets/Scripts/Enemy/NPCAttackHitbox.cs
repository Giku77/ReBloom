using UnityEngine;

/// <summary>
/// NPC 공격 히트박스 - 오른손 등 공격 부위에 붙이기
/// IsTrigger 활성화된 콜라이더 필요
/// </summary>
public class NPCAttackHitbox : MonoBehaviour
{
    [Header("Attack Settings")]
    public float damage = 50f;
   

    private void OnTriggerEnter(Collider other)
    {
        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        
        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
            
                Debug.Log($"[NPC 히트박스] 플레이어 타격! {damage} 데미지");
        }
    }
}