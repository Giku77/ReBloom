using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerOwnerGate : NetworkBehaviour
{
    [Header("Enable only for Owner")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInput playerInput;

    [Header("Physics (optional but recommended for 'movement only')")]
    [SerializeField] private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        bool isLocal = IsOwner; // 로컬 오너인지 :contentReference[oaicite:3]{index=3}

        if (playerController) playerController.enabled = isLocal;
        if (playerInput) playerInput.enabled = isLocal;

        // 이동만 “보이기” 단계에서 제일 덜 싸우는 설정:
        // 원격은 물리 끄고(NetworkTransform이 위치 적용), 로컬만 물리로 움직이게
        if (rb) rb.isKinematic = !isLocal;

        if (isLocal)
        {
            CameraRig.I?.Follow(transform);
        }
    }
}
