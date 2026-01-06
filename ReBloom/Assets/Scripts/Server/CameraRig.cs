using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public static CameraRig I { get; private set; }

    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private void Awake()
    {
        I = this;
    }

    public void Follow(Transform target)
    {

        if (!thirdPersonCamera) return;

        thirdPersonCamera.SetTarget(target);
    }
}
