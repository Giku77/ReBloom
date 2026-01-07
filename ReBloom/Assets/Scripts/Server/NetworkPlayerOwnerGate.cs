using System;
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

    public static event Action<GameObject> OnLocalPlayerSpawned;
    public static event Action OnLocalPlayerDespawned;

    public override void OnNetworkSpawn()
    {
        bool isLocal = IsOwner;

        Debug.Log($"[Gate] spawn name={name} netId={NetworkObjectId} owner={OwnerClientId} local={NetworkManager.Singleton.LocalClientId} IsOwner={IsOwner} IsLocalPlayer={IsLocalPlayer} IsServer={IsServer} IsClient={IsClient}");

        if (playerController) playerController.enabled = isLocal;
        if (playerInput) playerInput.enabled = isLocal;
        if (rb) rb.isKinematic = !isLocal;

        if (isLocal)
        {
            CameraRig.I?.Follow(transform);
            UIRoot.I?.BindLocalPlayer(playerController);
            OnLocalPlayerSpawned?.Invoke(gameObject);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            OnLocalPlayerDespawned?.Invoke();
    }
}
