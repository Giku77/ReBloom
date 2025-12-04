using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// 인벤토리 로봇 펫 메인 컨트롤러
/// 플레이어를 따라다니며 상황에 맞게 반응함
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class InventoryRobotPet : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField] private Transform player;                    // 플레이어 Transform
    [SerializeField] private float idleDistance = 2.0f;          // Idle 전환 거리
    [SerializeField] private float runDistance = 5.0f;           // Run 전환 거리
    [SerializeField] private float followHeight = 1.5f;          // 플레이어 위 얼마나 높이 떠있을지
    [SerializeField] private float teleportDistance = 15f;   // 이 거리 이상 떨어지면 텔레포트
    [SerializeField] private float teleportCooldown = 1.0f;  // 텔포 최소 간격

    public bool IsNearPlayer;
    private float lastTeleportTime = -999f;

    [Header("이동 속도")]
    [SerializeField] private float walkSpeed = 2.0f;             // 걷기 속도
    [SerializeField] private float runSpeed = 4.0f;              // 달리기 속도
    [SerializeField] private float rotationSpeed = 5.0f;         // 회전 속도

    [Header("둥둥 떠다니는 효과")]
    [SerializeField] private float floatAmplitude = 0.3f;        // 상하 움직임 크기
    [SerializeField] private float floatFrequency = 1.0f;        // 상하 움직임 속도

    [Header("컴포넌트 참조")]
    [SerializeField] private RobotAnimationController animController;
    [SerializeField] private RobotEmotionManager emotionManager;
    [SerializeField] private Rob11ColorManager colorManager;

    private bool isOrbiting = false;
    private float orbitAngle = 0f;
    private float orbitRadius = 2f;
    private float orbitSpeed = 90f;

    // 내부 상태
    private Rigidbody rb;
    private Animator animator;
    private RobotMovementState currentMovementState;
    private bool isPerformingAction = false;
    private float floatTimer = 0f;  // 둥둥 효과용 타이머

    // Input Action
    private InputAction interactAction;

    #region Unity 생명주기

    private void Awake()
    {
        // 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Rigidbody 설정 - 공중에 떠다니기
        rb.isKinematic = false;   // 물리 엔진 사용
        rb.useGravity = false;    // 중력 무시 (떠다니기 위해)
        rb.linearDamping = 2f;       // 공기 저항 (부드러운 이동)
        rb.angularDamping = 2f;      // 회전 저항
        rb.constraints = RigidbodyConstraints.FreezeRotation; // 회전은 코드로만 제어
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        if (interactAction != null)
        {
            interactAction.performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        if (interactAction != null)
        {
            interactAction.performed -= OnInteract;
        }
    }

    private void Start()
    {
        // 플레이어 자동 찾기
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // 시작 시 인사 애니메이션
        PlayGreeting();
    }

    private void FixedUpdate()
    {
        // 물리 연산은 FixedUpdate에서!
        if (isPerformingAction) return;

        FollowPlayer();
    }

    #region DeathBoxHandler 전용 외부 호출 함수
    public void TelePortTo() => TeleportToPlayer();
    #endregion

    private Vector3 CalCulateTelePortPosition()
    {
        if (player == null) return new Vector3(0,0,0);

        // 플레이어 기준 뒤쪽 + 위쪽 위치
        Vector3 playerPos = player.position;
        Vector3 backOffset = -player.forward * 2f; // 뒤로 2m
        Vector3 upOffset = Vector3.up * followHeight;

        Vector3 targetPos = playerPos + backOffset + upOffset;

        return targetPos;
    }
    private void TeleportToPlayer()
    {
        var targetPos = CalCulateTelePortPosition();
        // 순간이동
        rb.position = targetPos;
        transform.position = targetPos;

        // 속도/플로팅 타이머 리셋
        rb.linearVelocity = Vector3.zero;
        floatTimer = 0f;

        // 상태도 정리
        isOrbiting = false;
        ChangeMovementState(RobotMovementState.Idle);

        lastTeleportTime = Time.time;
    }

    #endregion

    #region 플레이어 추적 (둥둥 떠다니며)

    /// <summary>
    /// 플레이어를 향해 둥둥 떠다니며 이동
    /// </summary>
    // private void FollowPlayer()
    // {
    //     if (player == null || rb == null) return;

    //     // 플레이어와의 수평 거리 계산 (높이 무시)
    //     Vector3 playerPos = player.position;
    //     Vector3 currentPos = transform.position;
    //     Vector3 horizontalDirection = new Vector3(playerPos.x - currentPos.x, 0, playerPos.z - currentPos.z);
    //     float distanceToPlayer = horizontalDirection.magnitude;

    //     // 목표 위치 계산 (플레이어 위 일정 높이)
    //     Vector3 targetPosition = playerPos + Vector3.up * followHeight;

    //     // 둥둥 떠다니는 효과 추가 (사인파)
    //     floatTimer += Time.fixedDeltaTime * floatFrequency;
    //     float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;
    //     targetPosition.y += floatOffset;

    //     // 거리에 따른 상태 및 속도 결정
    //     float currentSpeed = 0f;

    //     if (distanceToPlayer <= idleDistance)
    //     {
    //         ChangeMovementState(RobotMovementState.Idle);
    //         currentSpeed = 0f;
    //     }
    //     else if (distanceToPlayer > idleDistance && distanceToPlayer <= runDistance)
    //     {
    //         ChangeMovementState(RobotMovementState.Walk);
    //         currentSpeed = walkSpeed;
    //     }
    //     else
    //     {
    //         ChangeMovementState(RobotMovementState.Run);
    //         currentSpeed = runSpeed;
    //     }

    //     // 이동 (Rigidbody 사용)
    //     if (currentSpeed > 0f)
    //     {
    //         Vector3 direction = (targetPosition - currentPos).normalized;
    //         Vector3 targetVelocity = direction * currentSpeed;

    //         // 부드러운 이동 (Lerp)
    //         rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 5f);

    //         // 플레이어 방향으로 회전 (부드럽게)
    //         if (horizontalDirection != Vector3.zero)
    //         {
    //             Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection);
    //             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
    //         }
    //     }
    //     else
    //     {
    //         // Idle 상태에서는 속도를 천천히 줄임
    //         rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 3f);
    //     }
    // }
    private void FollowPlayer()
    {
        if (player == null || rb == null) return;

        if (isOrbiting)
        {
            OrbitAroundPlayer();
            return;
        }

        Vector3 playerPos = player.position;
        Vector3 currentPos = transform.position;

        // 목표 위치 (플레이어 위 + 떠다니기)
        Vector3 targetPosition = playerPos + Vector3.up * followHeight;
        IsNearPlayer = true;

        floatTimer += Time.fixedDeltaTime * floatFrequency;
        float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;
        targetPosition.y += floatOffset;

        Vector3 toTarget = targetPosition - currentPos;
        float distanceToPlayer = toTarget.magnitude;

        if (distanceToPlayer > teleportDistance && Time.time - lastTeleportTime > teleportCooldown)
        {
            IsNearPlayer = false;
            animController.PlayAnimation("JumpForward");
            TeleportToPlayer();
            return;
        }

        float currentSpeed = 0f;

        if (distanceToPlayer <= idleDistance)
        {
            ChangeMovementState(RobotMovementState.Idle);
            currentSpeed = 0f;
        }
        else if (distanceToPlayer <= runDistance)
        {
            ChangeMovementState(RobotMovementState.Walk);
            currentSpeed = walkSpeed;
        }
        else
        {
            ChangeMovementState(RobotMovementState.Run);
            currentSpeed = runSpeed;
        }

        if (currentSpeed > 0f)
        {
            Vector3 direction = toTarget.normalized;
            Vector3 targetVelocity = direction * currentSpeed;

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 5f);

            // 수평 방향만 회전에 사용
            Vector3 horizontalDir = new Vector3(toTarget.x, 0, toTarget.z);
            if (horizontalDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }
        else
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 3f);
        }
    }

    public void StartOrbitingPlayer(float radius = 2f, float speed = 90f)
    {
        isOrbiting = true;
        orbitRadius = radius;
        orbitSpeed = speed;

        // 현재 각도 계산 (플레이어 기준)
        if (player != null)
        {
            Vector3 offset = transform.position - player.position;
            orbitAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        ChangeMovementState(RobotMovementState.Walk);
        emotionManager.SetEmotion(RobotEmotion.Wonder);
    }

    public void StopOrbitingPlayer()
    {
        isOrbiting = false;
        ChangeMovementState(RobotMovementState.Idle);
    }

    private void OrbitAroundPlayer()
    {
        if (player == null) return;

        // 각도 업데이트
        orbitAngle += orbitSpeed * Time.fixedDeltaTime;
        if (orbitAngle >= 360f) orbitAngle -= 360f;

        // 목표 위치 계산 (원형 궤도)
        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(radians) * orbitRadius,
            followHeight,
            Mathf.Cos(radians) * orbitRadius
        );

        Vector3 targetPosition = player.position + offset;

        // 둥둥 떠다니는 효과
        floatTimer += Time.fixedDeltaTime * floatFrequency;
        float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;
        targetPosition.y += floatOffset;

        // 부드럽게 이동
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 targetVelocity = direction * walkSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 5f);

        // 플레이어를 바라보도록 회전
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// 이동 상태 변경 및 애니메이션 조정
    /// </summary>
    private void ChangeMovementState(RobotMovementState newState)
    {
        if (currentMovementState == newState) return;

        currentMovementState = newState;

        switch (newState)
        {
            case RobotMovementState.Idle:
                animator.SetFloat("Speed", 0f);
                break;

            case RobotMovementState.Walk:
                animator.SetFloat("Speed", 0.5f);
                break;

            case RobotMovementState.Run:
                animator.SetFloat("Speed", 1.0f);
                animator.SetFloat("run", 1.0f);
                break;
        }
    }
    #endregion

    #region 이벤트 구독

    private void SubscribeToEvents()
    {
        //InventroyEventSystem.OnPlayerResting += HandlePlayerResting;
        //InventroyEventSystem.OnPlayerInDanger += HandlePlayerInDanger;
        //InventroyEventSystem.OnPlayerAttacked += HandlePlayerAttacked;
        //InventroyEventSystem.OnPlayerBuilding += HandlePlayerBuilding;
        //InventroyEventSystem.OnPlayerGathering += HandlePlayerGathering;
        //InventroyEventSystem.OnActionSuccess += HandleActionSuccess;
        //InventroyEventSystem.OnActionFailed += HandleActionFailed;
        //InventroyEventSystem.OnQuestCompleted += HandleQuestCompleted;

        //InventroyEventSystem.OnItemAcquired += HandleItemAdded;

        InventroyEventSystem.OnItemAcquiredTier += HandleItemAcquired;
        InventroyEventSystem.OnInventoryOpened += HandleInventoryOpened;
        //InventroyEventSystem.OnInventoryClosed += HandleInventoryClosed;
        InventroyEventSystem.OnInventoryFull += HandleInventoryFull;
        InventroyEventSystem.OnItemDropped += HandleItemDropped;
    }

    private void UnsubscribeFromEvents()
    {
        //InventroyEventSystem.OnPlayerResting -= HandlePlayerResting;
        //InventroyEventSystem.OnPlayerInDanger -= HandlePlayerInDanger;
        //InventroyEventSystem.OnPlayerAttacked -= HandlePlayerAttacked;
        //InventroyEventSystem.OnPlayerBuilding -= HandlePlayerBuilding;
        //InventroyEventSystem.OnPlayerGathering -= HandlePlayerGathering;
        //InventroyEventSystem.OnActionSuccess -= HandleActionSuccess;
        //InventroyEventSystem.OnActionFailed -= HandleActionFailed;
        //InventroyEventSystem.OnQuestCompleted -= HandleQuestCompleted;

        InventroyEventSystem.OnInventoryOpened -= HandleInventoryOpened;
        //InventroyEventSystem.OnInventoryClosed -= HandleInventoryClosed;
        //InventroyEventSystem.OnItemAcquired -= HandleItemAdded;
        InventroyEventSystem.OnItemAcquiredTier -= HandleItemAcquired;

        InventroyEventSystem.OnInventoryFull -= HandleInventoryFull;
        InventroyEventSystem.OnItemDropped -= HandleItemDropped;
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 플레이어가 휴식 중일 때 - 편안한 감정
    /// </summary>
    private void HandlePlayerResting()
    {
        emotionManager.SetEmotion(RobotEmotion.Happy);
        animController.PlayAnimation("Idle");
    }

    /// <summary>
    /// 플레이어가 위독한 상황 - 걱정하는 감정
    /// </summary>
    private void HandlePlayerInDanger()
    {
        emotionManager.SetEmotion(RobotEmotion.Cry);
        animController.PlayAnimation("Cry");
    }

    /// <summary>
    /// 플레이어가 공격받음 - 겁먹은 감정
    /// </summary>
    private void HandlePlayerAttacked()
    {
        emotionManager.SetEmotion(RobotEmotion.Evil);  // 또는 Distrust
        animController.PlayAnimation("No");
    }

    /// <summary>
    /// 아이템 획득 - Tier에 따라 다른 반응
    /// </summary>
    private void HandleItemAcquired(int tier)
    {
        if (tier >= 3)
        {
            // 고급 아이템 - 매우 기뻐함
            emotionManager.SetEmotion(RobotEmotion.Love);
            animController.PlayAnimation("Win");
        }
        else
        {
            // 일반 아이템 - 기뻐함
            emotionManager.SetEmotion(RobotEmotion.Happy);
            animController.PlayAnimation("Hit");
        }
    }

    /// <summary>
    /// 플레이어가 건축 중 - 궁금해하는 감정
    /// </summary>
    private void HandlePlayerBuilding()
    {
        emotionManager.SetEmotion(RobotEmotion.Wonder);
        animController.PlayAnimation("LookingFor");
    }

    /// <summary>
    /// 플레이어가 채집 중 - 관심있는 감정
    /// </summary>
    private void HandlePlayerGathering()
    {
        emotionManager.SetEmotion(RobotEmotion.Wonder);
        animController.PlayAnimation("Idle");
    }

    /// <summary>
    /// 건축/채집 성공 - 기뻐하는 감정
    /// </summary>
    private void HandleActionSuccess()
    {
        emotionManager.SetEmotion(RobotEmotion.Happy);
        animController.PlayAnimation("ThumbUp");
    }

    /// <summary>
    /// 채집 실패 - 실망하는 감정
    /// </summary>
    private void HandleActionFailed()
    {
        emotionManager.SetEmotion(RobotEmotion.Sad);
        animController.PlayAnimation("DontKnow");
    }

    /// <summary>
    /// 퀘스트 완료 - 축하하는 감정
    /// </summary>
    private void HandleQuestCompleted()
    {
        emotionManager.SetEmotion(RobotEmotion.Happy);
        animController.PlayAnimation("Dance0");
        colorManager.isRainbowCycles = true;
        Invoke(nameof(StopDance), 3f);  // 3초 후 춤 중지
    }

    /// <summary>
    /// 인벤토리 열림 - 점프로 반응
    /// </summary>
    private void HandleInventoryOpened()
    {
        animController.PlayAnimation("Jump");
    }

    /// <summary>
    /// 인벤토리 닫힘 - 점프로 반응
    /// </summary>
    //private void HandleInventoryClosed()
    //{
    //    animController.PlayAnimation("Jump");
    //}

    /// <summary>
    /// 인벤토리 가득참 - 거부 제스처
    /// </summary>
    private void HandleInventoryFull()
    {
        emotionManager.SetEmotion(RobotEmotion.Distrust);
        animController.PlayAnimation("No");
    }

    /// <summary>
    /// 아이템 버림 - 앞으로 돌진
    /// </summary>
    private void HandleItemDropped()
    {
        animController.PlayAnimation("JumpForward");
    }

    #endregion

    #region 특수 동작

    /// <summary>
    /// 시작 시 인사
    /// </summary>
    private void PlayGreeting()
    {
        emotionManager.SetEmotion(RobotEmotion.Happy);
        animController.PlayAnimation("Hello");
    }

    /// <summary>
    /// 춤 중지
    /// </summary>
    private void StopDance()
    {
        colorManager.isRainbowCycles = false;
        emotionManager.SetEmotion(RobotEmotion.Neutral);
    }

    /// <summary>
    /// I 상호작용 (예: 말하기)
    /// </summary>
    private void OnInteract(InputAction.CallbackContext context)
    {
        animController.PlayAnimation("EmoTalk1");
        emotionManager.SetEmotion(RobotEmotion.Neutral);
    }

    #endregion
}