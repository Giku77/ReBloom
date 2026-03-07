using Unity.Netcode;
using UnityEngine;

public class NPCAttackHitbox : MonoBehaviour
{
    [Header("Attack Settings")]
    public float damage = 50f;

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        NetworkPlayerOwnerGate gate = other.GetComponentInParent<NetworkPlayerOwnerGate>();
        if (gate == null)
            return;

        gate.ApplyAuthoritativeDamage(damage);
        Debug.Log($"[NPC 히트박스] 플레이어 타격! {damage} 데미지");
    }
}
