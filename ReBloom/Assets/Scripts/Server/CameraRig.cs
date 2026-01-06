using Unity.Netcode;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public static CameraRig I { get; private set; }

    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private void Awake()
    {
        I = this;
        if (!thirdPersonCamera)
            thirdPersonCamera = GetComponentInChildren<ThirdPersonCamera>(true);
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += HandleLocalPlayer;
        TryBindExistingLocalPlayer();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= HandleLocalPlayer;
    }

    private void HandleLocalPlayer(GameObject playerObj)
    {
        Follow(playerObj.transform);
    }

    private void TryBindExistingLocalPlayer()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null) return;

        var localNo = nm.SpawnManager.GetLocalPlayerObject();
        if (localNo != null)
            Follow(localNo.transform);
    }

    public void Follow(Transform target)
    {
        if (!thirdPersonCamera || !target) return;
        thirdPersonCamera.SetTarget(target);
    }
}
