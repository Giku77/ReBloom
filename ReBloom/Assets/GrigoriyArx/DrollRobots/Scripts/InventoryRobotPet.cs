using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class InventoryRobotPet : MonoBehaviour
{
    [Header("Follow (Proxy)")]
    [SerializeField] private Transform followTarget;           // RobotNavProxy Transform
    [SerializeField] private Transform player;
    [SerializeField] private float followHeight = 1.5f;

    [Header("Near Check")]
    [SerializeField] private float nearDistance = 6.0f;        // IsNearPlayer 기준

    [Header("Move State Dist")]
    [SerializeField] private float idleDistance = 2.0f;
    [SerializeField] private float runDistance = 5.0f;

    [Header("Move Speed")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 4.0f;
    [SerializeField] private float rotationSpeed = 5.0f;

    [Header("Floating")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatFrequency = 1.0f;

    [Header("Teleport FX")]
    [SerializeField] private SpawnEffect teleportFx;
    [SerializeField] private float teleportCooldown = 1.0f;

    [Header("Stuck Teleport (Progress-based)")]
    [SerializeField] private float stuckMinDistToProxy = 3.0f;
    [SerializeField] private float stuckNoImproveEps = 0.03f;   // 3cm 이상 가까워지지 않으면 "개선 없음"
    [SerializeField] private float stuckDelay = 0.8f;           // 이 시간 동안 개선 없으면 텔포
    [SerializeField] private float hardRecoverDistToProxy = 12f;

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitSpeed = 90f;

    [Header("Flashlight")]
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform lightSource;            // 로봇 머리/눈 위치(선택)
    [SerializeField] private float lightRange = 20f;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightSpotAngle = 60f;

    [Header("Refs")]
    [SerializeField] private RobotAnimationController animController;
    [SerializeField] private RobotEmotionManager emotionManager;
    [SerializeField] private Rob11ColorManager colorManager;
    [SerializeField] private Animator animator;

    [Header("Voice")]
    [SerializeField] private AudioSource poppyVoiceSource;
    [SerializeField] private int teleportVoiceChance = 10;
    private int teleportCount = 0;

    public bool IsNearPlayer { get; private set; }
    public void TelePortTo()
    {
        Vector3 basePos =
            (followTarget != null) ? followTarget.position :
            (player != null) ? player.position :
            transform.position;

        TeleportTo(basePos + Vector3.up * followHeight, true);
    }

    // 상태
    private Rigidbody rb;
    private RobotMovementState currentMovementState;
    private bool isPerformingAction = false;

    // floating
    private float floatTimer = 0f;

    // teleport
    private float lastTeleportTime = -999f;

    // stuck tracking
    private float stuckTime = 0f;
    private float lastDistToProxy = 999f;

    // proxy warp hook
    private RobotNavProxy proxy;

    // orbit
    private bool isOrbiting = false;
    private float orbitAngle = 0f;

    // flashlight
    private bool isFlashlightOn = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (flashlight != null)
        {
            flashlight.enabled = false;
            flashlight.range = lightRange;
            flashlight.intensity = lightIntensity;
            flashlight.spotAngle = lightSpotAngle;
        }

        if (poppyVoiceSource == null)
        {
            poppyVoiceSource = gameObject.AddComponent<AudioSource>();
            poppyVoiceSource.playOnAwake = false;
            poppyVoiceSource.spatialBlend = 1f;
            poppyVoiceSource.minDistance = 3f;
            poppyVoiceSource.maxDistance = 15f;
            poppyVoiceSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (followTarget == null)
            followTarget = player; // fallback

        proxy = followTarget != null ? followTarget.GetComponent<RobotNavProxy>() : null;
        if (proxy != null)
            proxy.OnWarp += HandleProxyWarp;

        PlayGreeting();
    }

    private void OnDestroy()
    {
        if (proxy != null)
            proxy.OnWarp -= HandleProxyWarp;
    }

    private void FixedUpdate()
    {
        if (isPerformingAction) return;

        UpdateIsNearPlayer();

        // 공전 중이면 공전 로직이 이동을 덮어씀
        if (isOrbiting)
        {
            OrbitAroundPlayer();
            return;
        }

        // stuck/복구 체크 (필요시 텔포)
        if (UpdateStuckAndRecover())
            return;

        FollowProxy();
    }

    private void UpdateIsNearPlayer()
    {
        if (player == null)
        {
            IsNearPlayer = false;
            return;
        }

        float d = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(player.position.x, 0, player.position.z)
        );

        IsNearPlayer = d <= nearDistance;
    }

    private void HandleProxyWarp(Vector3 proxyWarpPos)
    {
        // 프록시가 워프했으면 로봇도 같이 합류(연출 포함)
        TeleportTo(proxyWarpPos + Vector3.up * followHeight, true);
    }

    private bool UpdateStuckAndRecover()
    {
        if (followTarget == null) return false;

        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = followTarget.position; b.y = 0f;
        float dToProxy = Vector3.Distance(a, b);

        if (dToProxy > hardRecoverDistToProxy && Time.time - lastTeleportTime > teleportCooldown)
        {
            TeleportTo(followTarget.position + Vector3.up * followHeight, true);
            lastDistToProxy = 999f;
            stuckTime = 0f;
            return true;
        }

        float robotSpeed = rb.linearVelocity.magnitude; 
        bool robotMoving = robotSpeed > 0.25f;          

        float improveEps = 0.12f; 

        bool farEnough = dToProxy > stuckMinDistToProxy;
        bool notImproving = dToProxy >= (lastDistToProxy - improveEps);

        if (farEnough && !robotMoving && notImproving)
            stuckTime += Time.fixedDeltaTime;
        else
            stuckTime = 0f;

        lastDistToProxy = dToProxy;

        float delay = 1.6f; 

        if (stuckTime > delay && Time.time - lastTeleportTime > teleportCooldown)
        {
            TeleportTo(followTarget.position + Vector3.up * followHeight, true);
            stuckTime = 0f;
            return true;
        }

        return false;
    }


    private void FollowProxy()
    {
        if (rb == null || followTarget == null) return;

        Vector3 currentPos = transform.position;
        Vector3 basePos = followTarget.position;

        // 목표 높이 + 둥둥
        floatTimer += Time.fixedDeltaTime * floatFrequency;
        float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;
        Vector3 targetPosition = basePos + Vector3.up * (followHeight + floatOffset);

        Vector3 toTarget = targetPosition - currentPos;

        Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
        float distance = horizontal.magnitude;

        float currentSpeed = 0f;
        if (distance <= idleDistance)
        {
            ChangeMovementState(RobotMovementState.Idle);
            currentSpeed = 0f;
        }
        else if (distance <= runDistance)
        {
            ChangeMovementState(RobotMovementState.Walk);
            currentSpeed = walkSpeed;
        }
        else
        {
            ChangeMovementState(RobotMovementState.Run);
            currentSpeed = runSpeed;
        }

        if (currentSpeed > 0f && distance > 0.001f)
        {
            Vector3 dir = horizontal / distance;
            Vector3 targetVel = dir * currentSpeed;

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, Time.fixedDeltaTime * 2f);

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotationSpeed);
        }
        else
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 3f);
        }

        // Vector3 p = rb.position;
        // p.y = Mathf.Lerp(p.y, targetPosition.y, Time.fixedDeltaTime * 5f);
        Vector3 newPos = rb.position + (rb.linearVelocity * Time.fixedDeltaTime);

        newPos.y = Mathf.Lerp(rb.position.y, targetPosition.y, Time.fixedDeltaTime * 5f);

        rb.MovePosition(newPos);


        UpdateFlashlightPose();
    }

    private void TeleportTo(Vector3 worldPos, bool playFx)
    {
        rb.position = worldPos;
        transform.position = worldPos;
        rb.linearVelocity = Vector3.zero;

        if (player != null)
        {
            Vector3 look = player.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                rb.MoveRotation(Quaternion.LookRotation(look));
        }

        floatTimer = 0f;

        if (playFx)
            teleportFx?.PlayEffect();

        PlayerController playerController = player.GetComponent<PlayerController>();

        if (playerController != null && !playerController.IsInputBlocked)
        {
            teleportCount++;
            if (teleportCount >= teleportVoiceChance)
            {
                PlayPoppyVoiceBySituation(4);
                teleportCount = 0;
            }
        }

        stuckTime = 0f;
        lastDistToProxy = 999f;

        lastTeleportTime = Time.time;
        ChangeMovementState(RobotMovementState.Idle);

        // 텔포하면 공전은 풀기(원하면 유지 가능)
        isOrbiting = false;

        UpdateIsNearPlayer();
        UpdateFlashlightPose();
    }

    private void ChangeMovementState(RobotMovementState newState)
    {
        if (currentMovementState == newState) return;
        currentMovementState = newState;

        switch (newState)
        {
            case RobotMovementState.Idle:
                animator.SetFloat("Speed", 0f);
                animator.SetFloat("run", 0f);
                break;
            case RobotMovementState.Walk:
                animator.SetFloat("Speed", 0.5f);
                animator.SetFloat("run", 0f);
                break;
            case RobotMovementState.Run:
                animator.SetFloat("Speed", 1.0f);
                animator.SetFloat("run", 1.0f);
                break;
        }
    }

    private void PlayGreeting()
    {
        emotionManager?.SetEmotion(RobotEmotion.Happy);
        animController?.PlayAnimation("Hello");
    }

    public void SetPerformingAction(bool performing) => isPerformingAction = performing;

    public void StartOrbitingPlayer(float radius = 2f, float speed = 90f)
    {
        isOrbiting = true;
        orbitRadius = radius;
        orbitSpeed = speed;

        if (player != null)
        {
            Vector3 offset = transform.position - player.position;
            orbitAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        ChangeMovementState(RobotMovementState.Walk);
        emotionManager?.SetEmotion(RobotEmotion.Wonder);
    }

    public void StopOrbitingPlayer()
    {
        isOrbiting = false;
        ChangeMovementState(RobotMovementState.Idle);
    }

    private void OrbitAroundPlayer()
    {
        if (player == null || rb == null) return;

        orbitAngle += orbitSpeed * Time.fixedDeltaTime;
        if (orbitAngle >= 360f) orbitAngle -= 360f;

        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(radians) * orbitRadius,
            0f,
            Mathf.Cos(radians) * orbitRadius
        );

        floatTimer += Time.fixedDeltaTime * floatFrequency;
        float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;

        Vector3 targetPos = player.position + offset;
        targetPos.y = player.position.y + followHeight + floatOffset;

        // 이동(부드럽게)
        Vector3 to = targetPos - transform.position;
        Vector3 horizontal = new Vector3(to.x, 0, to.z);
        float dist = horizontal.magnitude;

        Vector3 vel = Vector3.zero;
        if (dist > 0.001f)
            vel = (horizontal / dist) * walkSpeed;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, vel, Time.fixedDeltaTime * 5f);

        // 플레이어 바라보기
        Vector3 look = player.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(look.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotationSpeed);
        }

        // 수직 스무스
        Vector3 p = rb.position;
        p.y = Mathf.Lerp(p.y, targetPos.y, Time.fixedDeltaTime * 5f);
        rb.position = p;
        transform.position = p;

        UpdateFlashlightPose();
    }

    public void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;
        SetFlashlight(isFlashlightOn);

        if (isFlashlightOn)
        {
            emotionManager?.SetEmotion(RobotEmotion.Wonder);
            animController?.PlayAnimation("LookingFor");
        }
    }

    public void SetFlashlight(bool on)
    {
        isFlashlightOn = on;
        if (flashlight != null)
            flashlight.enabled = on;

        UpdateFlashlightPose();
    }

    private void UpdateFlashlightPose()
    {
        if (!isFlashlightOn || flashlight == null || player == null) return;

        // 손전등 위치(옵션: lightSource 있으면 그걸 사용)
        Transform src = (lightSource != null) ? lightSource : transform;
        flashlight.transform.position = src.position;

        // 기본은 플레이어 쪽 비추기(원하면 카메라 전방으로 바꿔도 됨)
        Vector3 dir = (player.position + Vector3.up * 1.0f) - src.position;
        if (dir.sqrMagnitude > 0.001f)
            flashlight.transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    public void PlayPoppyVoice(int varcoId)
    {
        if (poppyVoiceSource == null)
            return;

        // VoiceManager에서 AudioClip 가져오기
        var clip = VoiceManager.I?.GetAudioClip(varcoId);
        if (clip != null)
        {
            poppyVoiceSource.clip = clip;
            poppyVoiceSource.Play();
        }
    }

    public void PlayPoppyVoiceBySituation(int situation)
    {
        if (VoiceManager.I == null) return;

        int varcoId = VoiceManager.I.GetRandomVarcoIdBySituation(situation);
        if (varcoId > 0)
        {
            PlayPoppyVoice(varcoId);
        }
    }
}
