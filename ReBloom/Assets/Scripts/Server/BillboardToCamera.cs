using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam) return;
        transform.forward = cam.transform.forward;
    }
}
