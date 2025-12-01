using UnityEngine;

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