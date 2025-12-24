using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform target;
    public float distance = 10f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float mouseSensitivity = 3f;
    private float touchSensitivity = 0.3f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;
    [SerializeField] private float zoomSpeed = 0.5f;
    public bool isZoomLocked = false;
    public bool isSequenceLocked = false;

    [Header("UI Areas")]
    [SerializeField] private RectTransform joystickArea; // 조이스틱 영역
    [SerializeField] private RectTransform[] uiAreas; // 추가 UI 영역들 (버튼 등)

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

    // EnhancedTouch용 카메라 터치 추적
    private Touch? cameraTouch = null;

    // 플랫폼 정보 캐싱
    private bool isMobile;
    private bool isPC;

    private void Awake()
    {
        if (PlatformManager.Instance != null)
        {
            isMobile = PlatformManager.Instance.IsMobile;
            isPC = PlatformManager.Instance.IsPC;
            Debug.Log($"[ThirdPersonCamera] 플랫폼 초기화: IsMobile={isMobile}, IsPC={isPC}");
        }
        else
        {
            isMobile = Application.isMobilePlatform;
            isPC = !isMobile;
            Debug.LogWarning("[ThirdPersonCamera] PlatformManager가 없습니다!");
        }
    }

    private void Start()
    {
        var cam = Camera.main;
        cam.farClipPlane = 150f;

        SettingManager.I.OnMouseSensitivityChanged += UpdateSensitivity;
    }

    private void LateUpdate()
    {
        if (isMobile)
        {
            HandleMobileTouch();
        }

        Look();

        if (!isZoomLocked && !isSequenceLocked)
        {
            HandleZoom();
        }
    }

    private void OnEnable()
    {
        if (isMobile)
        {
            EnhancedTouchSupport.Enable();
        }

        StageDetector.OnEnterDoor += EnterInside;
        SettingManager.I.OnMouseSensitivityChanged += UpdateSensitivity;
    }

    private void OnDisable()
    {
        if (isMobile)
        {
            EnhancedTouchSupport.Disable();
        }

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

    //인풋시스템 콜백함수 룩
    public void OnLook(InputAction.CallbackContext context)
    {
        if (isMobile)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            lookInput = Vector2.zero;
            return;
        }

        lookInput = context.ReadValue<Vector2>();
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

            if (isPC)
            {
                yaw += lookInput.x * mouseSensitivity * Time.deltaTime;
                pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
            }
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
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

    #region Mobile Touch Handling
    //private void HandleMobileTouch()
    //{
    //    // 기존 터치 처리
    //    if (cameraTouch.HasValue)
    //    {
    //        Touch touch = cameraTouch.Value;

    //        // 터치 종료 확인
    //        if (!touch.valid || touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
    //        {
    //            cameraTouch = null;
    //            Debug.Log("[Camera] 터치 종료");
    //            return;
    //        }

    //        // 현재 터치가 UI 영역으로 이동했는지 체크
    //        if (IsTouchOverUI(touch))
    //        {
    //            Debug.Log("[Camera] UI 영역으로 이동 - 카메라 회전 중단");
    //            return; // UI 위로 이동하면 회전 안 함
    //        }

    //        // UI 밖에서 움직이는 중이면 카메라 회전
    //        if (touch.phase == TouchPhase.Moved)
    //        {
    //            Vector2 delta = touch.delta;
    //            RotateCameraByTouch(delta);
    //        }
    //    }

    //    // 새로운 터치 감지
    //    if (!cameraTouch.HasValue)
    //    {
    //        foreach (Touch touch in Touch.activeTouches)
    //        {
    //            if (touch.phase == TouchPhase.Began)
    //            {
    //                // 터치 시작 시 UI 체크
    //                if (IsTouchOverUI(touch))
    //                {
    //                    Debug.Log("[Camera] UI 터치 시작 - 무시");
    //                    continue;
    //                }

    //                cameraTouch = touch;
    //                Debug.Log($"[Camera] 카메라 드래그 시작 - 위치: {touch.screenPosition}");
    //                break;
    //            }
    //        }
    //    }
    //}

    #region Mobile Touch Handling
    private void HandleMobileTouch()
    {
        // 기존 카메라 터치 처리
        if (cameraTouch.HasValue)
        {
            Touch currentTouch = cameraTouch.Value;
            bool touchFound = false;

            // activeTouches에서 같은 finger를 찾아서 최신 정보로 업데이트
            foreach (Touch touch in Touch.activeTouches)
            {
                if (touch.finger.index == currentTouch.finger.index)
                {
                    currentTouch = touch;
                    touchFound = true;
                    break;
                }
            }

            // 터치가 끝났거나 사라졌으면 리셋
            if (!touchFound ||
                currentTouch.phase == TouchPhase.Ended ||
                currentTouch.phase == TouchPhase.Canceled)
            {
                cameraTouch = null;
                Debug.Log("[Camera] 터치 종료");
            }
            else
            {
                // UI 영역으로 이동했는지 체크
                if (IsTouchOverUI(currentTouch))
                {
                    Debug.Log("[Camera] UI 영역으로 이동 - 카메라 회전 중단");
                }
                else if (currentTouch.phase == TouchPhase.Moved)
                {
                    // 카메라 회전 처리
                    Vector2 delta = currentTouch.delta;
                    RotateCameraByTouch(delta);
                }

                // 최신 터치 정보 저장
                cameraTouch = currentTouch;
                return;
            }
        }

        // 새로운 카메라 터치 찾기
        foreach (Touch touch in Touch.activeTouches)
        {
            Debug.Log($"[Camera] 터치 검사 - Finger: {touch.finger.index}, Phase: {touch.phase}, Pos: {touch.screenPosition}");

            // UI가 아닌 터치를 찾으면 카메라 터치로 등록
            if (!IsTouchOverUI(touch))
            {
                cameraTouch = touch;
                Debug.Log($"[Camera] ✅ 카메라 터치 등록! Finger: {touch.finger.index}");
                return;
            }
        }

        Debug.Log("[Camera] 카메라 터치를 찾지 못함 (모두 UI)");
    }

    private bool IsTouchOverUI(Touch touch)
    {
        Vector2 touchPos = touch.screenPosition;

        // RaycastAll로 정확히 체크
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = touchPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // WorldDropZone 제외하고 실제 UI만 체크
        bool hasRealUI = false;
        if (results.Count > 0)
        {
            foreach (var result in results)
            {
                string objName = result.gameObject.name;

                // WorldDropZone은 무시
                if (objName == "WorldDropZone")
                {
                    continue;
                }

                // 실제 UI 발견
                Debug.Log($"[Camera] Finger {touch.finger.index} UI 감지: {objName}");
                hasRealUI = true;
                break;
            }
        }

        if (hasRealUI)
        {
            return true;
        }

        // 조이스틱 영역 직접 체크
        if (joystickArea != null && IsPointInRectTransform(joystickArea, touchPos))
        {
            Debug.Log($"[Camera] 조이스틱 영역");
            return true;
        }

        // 추가 UI 영역들 체크
        if (uiAreas != null)
        {
            foreach (var uiArea in uiAreas)
            {
                if (uiArea != null && IsPointInRectTransform(uiArea, touchPos))
                {
                    Debug.Log($"[Camera] UI 영역: {uiArea.name}");
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPointInRectTransform(RectTransform rectTransform, Vector2 screenPoint)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            screenPoint,
            null
        );
    }

    private void RotateCameraByTouch(Vector2 delta)
    {
        if (isSequenceLocked)
            return;

        float currentSensitivity = touchSensitivity;

        if (BuildPlacementController.I != null && BuildPlacementController.I.IsPlacing)
        {
            currentSensitivity *= 0.3f;
        }

        yaw += delta.x * currentSensitivity;
        pitch -= delta.y * currentSensitivity;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }
    #endregion
    #endregion
}