using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
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
    private HashSet<int> joystickFingerIndices = new HashSet<int>();

    // 플랫폼 정보 캐싱
    private bool isMobile;
    private bool isPC;

    private bool isPlayingSequence;
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
        if (isPlayingSequence) return;
        if (isMobile)
        {
            HandleMobileTouch();
            HandlePinchZoom();
        }

        Look();

        if (!isZoomLocked && !isSequenceLocked)
        {
            HandleZoom();
        }
    }

    private int _sequenceToken = 0;

    public void PlayFocusSequenceUniTask(
        Vector3 focusLookAtWorld,
        Vector3 cameraPosWorld,
        float blendIn = 0.35f,
        float hold = 0.6f,
        float blendOut = 0.35f,
        Action onMidAction = null
    )
    {
        // fire-and-forget
        _ = PlayFocusSequenceAsync(focusLookAtWorld, cameraPosWorld, blendIn, hold, blendOut, onMidAction);
    }

    private async UniTaskVoid PlayFocusSequenceAsync(
        Vector3 lookAtWorld,
        Vector3 camPosWorld,
        float blendIn,
        float hold,
        float blendOut,
        Action onMidAction
    )
    {
        if (isPlayingSequence) return;
        isPlayingSequence = true;

        int myToken = ++_sequenceToken;
        var ct = this.GetCancellationTokenOnDestroy();

        // 현재 카메라 포즈 저장
        Vector3 savedPos = transform.position;
        Quaternion savedRot = transform.rotation;

        // 입력/줌 잠금
        bool prevZoomLocked = isZoomLocked;
        bool prevSeqLocked = isSequenceLocked;
        isZoomLocked = true;
        isSequenceLocked = true;

        try
        {
            // 목표 포즈 계산
            Vector3 seqFromPos = savedPos;
            Quaternion seqFromRot = savedRot;

            Vector3 seqToPos = camPosWorld;
            Vector3 dir = (lookAtWorld - seqToPos);
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            Quaternion seqToRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            // --- Blend In ---
            await LerpPoseAsync(seqFromPos, seqFromRot, seqToPos, seqToRot, blendIn, myToken, ct);

            // 펜스 OFF 같은 핵심 액션
            onMidAction?.Invoke();

            // --- Hold ---
            if (hold > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(hold), cancellationToken: ct);

            // --- Blend Out ---
            await LerpPoseAsync(seqToPos, seqToRot, savedPos, savedRot, blendOut, myToken, ct);
        }
        catch (OperationCanceledException)
        {
            // 파괴되면 그냥 종료
        }
        finally
        {
            // 잠금 해제 
            isZoomLocked = prevZoomLocked;
            isSequenceLocked = prevSeqLocked;

            isPlayingSequence = false;
        }
    }

    private async UniTask LerpPoseAsync(
        Vector3 fromPos, Quaternion fromRot,
        Vector3 toPos, Quaternion toRot,
        float duration,
        int myToken,
        CancellationToken ct
    )
    {
        if (duration <= 0.0001f)
        {
            transform.position = toPos;
            transform.rotation = toRot;
            return;
        }

        float t = 0f;
        while (t < 1f)
        {
            if (myToken != _sequenceToken) return;

            ct.ThrowIfCancellationRequested();

            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(fromPos, toPos, u);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, u);

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
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

    private void HandlePinchZoom()
    {
        if (isZoomLocked) return;

        List<Touch> validTouches = new List<Touch>();
        foreach (Touch touch in Touch.activeTouches)
        {
            if (joystickFingerIndices.Contains(touch.finger.index))
            {
                continue;
            }

            if (!IsTouchOverUI(touch))
            {
                validTouches.Add(touch);
            }
        }

        if (validTouches.Count != 2)
            return;

        Touch touch0 = validTouches[0];
        Touch touch1 = validTouches[1];

        if ((touch0.phase != TouchPhase.Moved && touch0.phase != TouchPhase.Stationary) ||
            (touch1.phase != TouchPhase.Moved && touch1.phase != TouchPhase.Stationary))
        {
            return;
        }

        float currentDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);

        Vector2 touch0PrevPos = touch0.screenPosition - touch0.delta;
        Vector2 touch1PrevPos = touch1.screenPosition - touch1.delta;
        float previousDistance = Vector2.Distance(touch0PrevPos, touch1PrevPos);

        float deltaDistance = currentDistance - previousDistance;

        if (Mathf.Abs(deltaDistance) < 1f)
            return;

        float zoomAmount = deltaDistance * zoomSpeed * 0.01f;
        distance -= zoomAmount;
        distance = Mathf.Clamp(distance, maxZoominDistance, maxZoomOutDistance);
    }

    private void UpdateJoystickTouches()
    {
        joystickFingerIndices.RemoveWhere(fingerIndex =>
        {
            foreach (Touch touch in Touch.activeTouches)
            {
                if (touch.finger.index == fingerIndex)
                    return false; // 아직 활성 터치
            }
            return true; // 종료된 터치
        });

        foreach (Touch touch in Touch.activeTouches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (joystickArea != null && IsPointInRectTransform(joystickArea, touch.screenPosition))
                {
                    joystickFingerIndices.Add(touch.finger.index);
                    Debug.Log($"[Camera] 조이스틱 터치 추가: finger={touch.finger.index}");
                }
            }
        }
    }

    private void HandleMobileTouch()
    {
        UpdateJoystickTouches();

        if (cameraTouch.HasValue)
        {
            Touch currentTouch = cameraTouch.Value;
            bool touchFound = false;

            foreach (Touch touch in Touch.activeTouches)
            {
                if (touch.finger.index == currentTouch.finger.index)
                {
                    currentTouch = touch;
                    touchFound = true;
                    break;
                }
            }

            if (!touchFound ||
                currentTouch.phase == TouchPhase.Ended ||
                currentTouch.phase == TouchPhase.Canceled)
            {
                cameraTouch = null;
            }
            else
            {
                if (IsTouchOverUI(currentTouch))
                {
                }
                else if (currentTouch.phase == TouchPhase.Moved)
                {

                    Vector2 delta = currentTouch.delta;
                    RotateCameraByTouch(delta);
                }

                cameraTouch = currentTouch;
                return;
            }
        }

        foreach (Touch touch in Touch.activeTouches)
        {
            if (joystickFingerIndices.Contains(touch.finger.index))
            {
                continue;
            }

            if (touch.phase == TouchPhase.Began)
            {
                if (!IsTouchOverUI(touch))
                {
                    cameraTouch = touch;
                    return;
                }
            }
        }
    }

    private bool IsTouchOverUI(Touch touch)
    {
        Vector2 touchPos = touch.screenPosition;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = touchPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool hasRealUI = false;
        if (results.Count > 0)
        {
            foreach (var result in results)
            {
                string objName = result.gameObject.name;

                if (objName == "WorldDropZone")
                {
                    continue;
                }

                hasRealUI = true;
                break;
            }
        }

        if (hasRealUI)
        {
            return true;
        }

        if (joystickArea != null && IsPointInRectTransform(joystickArea, touchPos))
        {
            return true;
        }

        if (uiAreas != null)
        {
            foreach (var uiArea in uiAreas)
            {
                if (uiArea != null && IsPointInRectTransform(uiArea, touchPos))
                {
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
}