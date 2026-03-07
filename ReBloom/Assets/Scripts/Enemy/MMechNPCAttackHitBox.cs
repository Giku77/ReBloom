using Unity.Netcode;
using UnityEngine;

public class MMechNPCAttackHitBox : MonoBehaviour
{
    private MMechBlueNPCController controller;

    private void Start()
    {
        controller = GetComponentInParent<MMechBlueNPCController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        NetworkPlayerOwnerGate gate = other.GetComponentInParent<NetworkPlayerOwnerGate>();
        if (gate == null)
            return;

        gate.ApplyAuthoritativeStun(controller.stunTime);
    }
}
