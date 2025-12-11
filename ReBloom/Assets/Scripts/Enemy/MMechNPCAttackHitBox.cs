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
        PlayerController playerController = other.GetComponent<PlayerController>();

        if (playerController != null)
        {
            playerController.ApplyStun(controller.stunTime);
        }
    }
}
