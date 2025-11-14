using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;
    [SerializeField] private float maxZoomOutDistance = 20f;
    [SerializeField] private float maxZoominDistance = 4f;

    [SerializeField] private LayerMask collisionMask;

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector2 lookInput;

    private Quaternion oldRotation;

    private void LateUpdate()
    {
        Look();
    }

    //인풋시스템 콜바이함수 룩
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        Vector2 scrollDelta = context.ReadValue<Vector2>();
        distance -= scrollDelta.y;
        distance = Mathf.Clamp(distance, maxZoominDistance, maxZoomOutDistance);
    }

    //시야 이동 함수
    private void Look()
    {
        //if (Cursor.lockState != CursorLockMode.Locked)
        //    return;

        //if (target == null) return;
        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                return;
            }
        }

        yaw += lookInput.x * mouseSensitivity * Time.deltaTime;
        pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            oldRotation = rotation;
        }
        else
        { 
            rotation = oldRotation;
        
        }
            //Vector3 offset = rotation * new Vector3(0f, height, -distance);

            //transform.position = target.position + offset;
            //transform.LookAt(target.position + Vector3.up * height);

        Vector3 desiredPosition = target.position + rotation * new Vector3(0f, height, -distance);
        Vector3 playerEye = target.position + Vector3.up * height;
        RaycastHit hit;

        if (Physics.Linecast(playerEye, desiredPosition, out hit, collisionMask))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = desiredPosition;
        }

        transform.LookAt(playerEye);
    }
}