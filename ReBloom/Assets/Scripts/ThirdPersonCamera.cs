using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform target;
    public float distance = 10f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;
    [SerializeField] private float zoomSpeed = 0.5f;
    public bool isZoomLocked = false;
    public bool isSequenceLocked = false;

    private float maxZoomOutDistance = 20f;
    private float maxZoominDistance = 1f;
    private float maxZoomOutInsideDistance = 2.9f;
    private float originalMaxZoomOutDistance = 20f;

    [SerializeField] private LayerMask collisionMask;

    private float yaw = 0f;
    private float pitch = 0f;
    private float savedYaw;
    private float savedPitch;
    private Vector2 lookInput;

    private Quaternion oldRotation;

    private float outsideDistance;

    private bool isPointerOverUI = false;

    private void Start()
    {
        var cam = Camera.main;
        cam.farClipPlane = 150f;

        SettingManager.I.OnMouseSensitivityChanged += UpdateSensitivity;
    }

    private void LateUpdate()
    {
        Look();

        if (!isZoomLocked && !isSequenceLocked)
        {
            HandleZoom();
        }
    }

    private void OnEnable()
    {
        StageDetector.OnEnterDoor += EnterInside;
        SettingManager.I.OnMouseSensitivityChanged += UpdateSensitivity;
    }

    private void OnDisable()
    {
        StageDetector.OnEnterDoor -= EnterInside;
        SettingManager.I.OnMouseSensitivityChanged -= UpdateSensitivity;
    }

    private void EnterInside(bool isInside)
    {
        if (isInside)
        {
            outsideDistance = distance;

            if (distance > maxZoomOutInsideDistance)
            {
                distance = maxZoomOutInsideDistance;
            }

            maxZoomOutDistance = maxZoomOutInsideDistance;
        }
        else
        {
            maxZoomOutDistance = originalMaxZoomOutDistance;

            distance = outsideDistance;

            outsideDistance = 0f;
        }
    }

    //인풋시스템 콜바이함수 룩
    public void OnLook(InputAction.CallbackContext context)
    {
        //if (Cursor.lockState != CursorLockMode.Locked)
        //{
        //    lookInput = Vector2.zero;
        //    return;
        //}

        //lookInput = context.ReadValue<Vector2>();

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (context.phase == InputActionPhase.Started)
            {
                isPointerOverUI = IsPointerOverUIElement();
            }

            if (isPointerOverUI)
            {
                lookInput = Vector2.zero;
                return;
            }
        }

        lookInput = context.ReadValue<Vector2>();
    }

    private bool IsPointerOverUIElement()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
        }
        else if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        return false;
    }

    private void HandleZoom()
    {
        if (Mouse.current == null) return;

        Vector2 scroll = Mouse.current.scroll.ReadValue();

        if (Mathf.Abs(scroll.y) < 0.01f)
            return;

        distance -= scroll.y * zoomSpeed * 0.1f;
        distance = Mathf.Clamp(distance, maxZoominDistance, maxZoomOutDistance);
    }

    public void AddMouseSensitivity(float sensitivity)
    {
        mouseSensitivity += sensitivity;
    }

    public void SubMouseSensitivity(float sensitivity)
    {
        mouseSensitivity -= sensitivity;
        if (mouseSensitivity < 1f)
            mouseSensitivity = 1f;
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        Vector2 scrollDelta = context.ReadValue<Vector2>();
        distance -= scrollDelta.y / 2;
        distance = Mathf.Clamp(distance, maxZoominDistance, maxZoomOutDistance);
    }

    //시야 이동 함수
    private void Look()
    {
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

        if (!isSequenceLocked)
        {
            float currentSensitivity = mouseSensitivity;
            if (BuildPlacementController.I != null && BuildPlacementController.I.IsPlacing)
            {
                currentSensitivity *= 0.3f;
            }

            yaw += lookInput.x * mouseSensitivity * Time.deltaTime;
            pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        rotation = Quaternion.Euler(pitch, yaw, 0f);
        oldRotation = rotation;


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

    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, maxZoominDistance, maxZoomOutDistance);
    }

    public void SetCameraAngle(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = Mathf.Clamp(newPitch, minVerticalAngle, maxVerticalAngle);
    }

    public void EnterFreeLook()
    {
        savedYaw = yaw;
        savedPitch = pitch;
    }

    public void ExitFreeLook()
    {
        yaw = savedYaw;
        pitch = savedPitch;
    }

    private Transform savedTarget;
    private float savedDistance;
    private float savedHeight;
    private bool savedZoomLocked;
    private bool savedSequenceLocked;

    public void EnterTopDown(Transform focus, float topDistance = 6f, float topHeight = 0f)
    {
        if (focus == null) return;

        savedTarget = target;
        savedDistance = distance;
        savedHeight = height;
        savedYaw = yaw;
        savedPitch = pitch;
        savedZoomLocked = isZoomLocked;
        savedSequenceLocked = isSequenceLocked;

        target = focus;
        distance = topDistance;
        height = topHeight;

        isZoomLocked = true;
        isSequenceLocked = true;

        pitch = 89f;
        yaw = focus.eulerAngles.y;

        LookImmediate();
    }

    public void ExitTopDown()
    {
        target = savedTarget;
        distance = savedDistance;
        height = savedHeight;
        yaw = savedYaw;
        pitch = savedPitch;
        isZoomLocked = savedZoomLocked;
        isSequenceLocked = savedSequenceLocked;

        LookImmediate();
    }

    private void LookImmediate()
    {
        Look();
    }

    private void UpdateSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
}