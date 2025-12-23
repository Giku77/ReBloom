using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    private float sprintSpeed;

    [NonSerialized] public float originalSpeed;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float turnSpeed = 15f;
    [SerializeField] private float slowSpeed = 4f;
    [SerializeField] private float changeSpeedRadius = 4;

    [Header("Step Climb Settings")]
    [SerializeField] private float stepHeight = 0.4f;      // 올라갈 수 있는 최대 턱 높이
    [SerializeField] private float stepRayLength = 0.5f;   // 앞쪽 감지 거리
    [SerializeField] private LayerMask stepLayerMask;      // 부딪힐 지면/장애물 레이어 (groundLayer랑 같게 써도 됨)
    [SerializeField] private float stepSmooth = 0.1f;      // 한 프레임에 얼마나 올릴지

    [Header("References")]
    [SerializeField] private EquipmentUI equipmentUI;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private InventoryRobotPet robotPet;

    public event Action onPassOut;

    private float originalRotationSpeed;
    private float originalTurnSpeed;
    private bool wasBuildPlacing = false;
    private float originalZoomDistance;

    public float currentSpeed = 0f;

    public float SpeedRatio
    {
        get
        {
            if (originalSpeed <= 0f) return 1f;
            return moveSpeed / originalSpeed;   // 1.0f == 100%
        }
    }

    public int SpeedPercent
    {
        get
        {
            return Mathf.RoundToInt(SpeedRatio * 100f);
        }
    }

    private float targetSpeed;

    private bool isSprinting = false;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    public bool isSlow = false;
    private bool isFreeLook = false;

    public bool isDead = false;
    private bool isStunned = false;
    private float stunDuration;
    private float stunTime = 0f;

    private bool isInputBlocked = false;

    private bool IsMovementLocked => isDead || isInteracting || isInputBlocked;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    //임시 장착 확인용 
    [Header("Equipment")]
    // [SerializeField] private InventoryItemData inventory;
    public PlayerEquipManager playerEquip;

    //LSY: 읽기 전용 인벤토리
    [SerializeField] GameInventory inventory;
    public GameInventory Inventory => inventory;

    [Header("Jump Setting")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Drop Setting")]
    [SerializeField] private float minDropHeight = 3f;
    [SerializeField] private float maxDropHeight = 15f;
    [SerializeField] private float landingSlow = 0.2f;

    private Rigidbody rb;

    private bool jumpRequested = false;

    public bool JumpRequested => jumpRequested;

    public PlayerAnimation Anim { get; private set; }

    private bool isAutoRun = false;
    private bool isGround = false;
    private bool wasJumping = false;
    public bool WasJumping => wasJumping;
    public bool isInteracting = false;

    [Header("Debug")]
    [SerializeField] private float debugSpeed = 15f;
    private bool debugMode = false;

    public PlayerStats playerStats;

    private float highestY;
    private bool wasGround = false;

    [SerializeField] Transform spawnPoint;

    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private WaterTankUI waterTankUI;

    [SerializeField] private StorageUI storageUI;
    [SerializeField] private float storageCloseDistance = 5f;

    private Vector2 mobileInput;
    private bool mobileIsSprinting;

    public void OpenCraftingUI()
    {
        if (craftingUI != null)
            craftingUI.Toggle();
    }

    public void OpenWaterTankUI()
    {
        if (waterTankUI != null)
            waterTankUI.Toggle();
    }

    public WorldStorage CurrentOpenedStorage { get; private set; }

    /// <summary>
    /// 창고 참조 설정 (WorldStorage.Interact에서 호출)
    /// </summary>
    public void SetCurrentStorage(WorldStorage storage)
    {
        CurrentOpenedStorage = storage;

        if (storage != null)
        {
            Debug.Log($"[PlayerController] 창고 설정: {storage.name}");
        }
        else
        {
            Debug.Log("[PlayerController] 창고 참조 제거");
        }
    }

    /// <summary>
    /// 창고와의 거리 체크 (Update에서 호출)
    /// </summary>
    private void CheckStorageDistance()
    {
        if (CurrentOpenedStorage == null) return;

        float dist = Vector3.Distance(
            transform.position,
            CurrentOpenedStorage.transform.position
        );

        //Debug.Log($"[PlayerController] 창고 거리: {dist:F2}m");

        if (dist > storageCloseDistance)
        {
            //Debug.Log($"[PlayerController] 창고가 너무 멀어짐! UI 닫기");

            // WorldStorage에게 닫으라고 요청
            CurrentOpenedStorage.CloseUI();
            CurrentOpenedStorage = null;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.Log("No Camera");
        }

        Anim = GetComponent<PlayerAnimation>();
        playerStats = GetComponent<PlayerStats>();
        playerEquip = GetComponent<PlayerEquipManager>();
        inventory = FindFirstObjectByType<GameInventory>();
    }

    private void Start()
    {
        sprintSpeed = moveSpeed * 1.5f;
        originalSpeed = moveSpeed;

        originalRotationSpeed = rotationSpeed;
        originalTurnSpeed = turnSpeed;

        if (playerStats != null)
            playerStats.OnDeath += HandleDeath;

        SoundManager.I?.PlayMainBGM();
    }

    /// <summary>
    /// 인벤토리 확장 (확장칩용)
    /// </summary>
    /// <param name="slotType">확장할 인벤토리 타입</param>
    /// <param name="targetTier">목표 Tier (1, 2, 3)</param>
    /// 
    public bool TryExpandInventoryWithChip(int tier)
    {
        if (Inventory == null)
        {
            Debug.LogError("[PlayerController] Inventory 없음");
            return false;
        }

        return Inventory.TryExpandWithChip(tier);
    }
    //public bool ExpandInventory(int targetTier)
    //{
    //    if (Inventory == null)
    //    {
    //        Debug.LogError($"[PlayerController] 존재하지 않는 인벤토리: {Inventory}");
    //        return false;
    //    }

    //    bool success = Inventory.Expand(targetTier);

    //    if (success)
    //    {
    //        int newSlots = inventoryItemData.SlotCount;
    //        //Debug.Log($"[PlayerController] {inventory} 인벤토리 Tier {targetTier}로 확장 완료! (현재 {newSlots}칸)");

    //        // TODO: 토스트 메시지 표시
    //        // ToastManager.I?.Show($"{slotType} 인벤토리 Tier {targetTier} 확장!");
    //    }

    //    return success;
    //}

    ///// <summary>
    ///// 다음 Tier로 업그레이드
    ///// </summary>
    //public bool ExpandInventoryToNextTier(InventorySlotType slotType)
    //{
    //    if (inventoryItemData == null)
    //    {
    //        Debug.LogError($"[PlayerController] 존재하지 않는 인벤토리: {inventoryItemData}");
    //        return false;
    //    }

    //    return inventoryItemData.ExpandToNextTier();
    //}

    private readonly Dictionary<object, float> speedMultipliers = new();
    public void AddSpeedMultiplier(object source, float multiplier)
    {
        speedMultipliers[source] = multiplier;
        RecalculateMoveSpeed();
        equipmentUI?.UpdateResistText();
    }

    public void RemoveSpeedMultiplier(object source)
    {
        if (speedMultipliers.Remove(source))
        {
            RecalculateMoveSpeed();
        }
        equipmentUI?.UpdateResistText();
    }

    private void RecalculateMoveSpeed()
    {
        float speed = originalSpeed;

        foreach (var mul in speedMultipliers.Values)
            speed *= mul;

        moveSpeed = speed;
        sprintSpeed = moveSpeed * 1.5f;  
    }

    private void StepClimb()
    {
        if (!isGround) return;
        if (moveDirection.sqrMagnitude < 0.01f) return;

        Vector3 originLow = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(originLow, transform.forward, out RaycastHit hitLow, stepRayLength, stepLayerMask))
        {
            Vector3 originHigh = transform.position + Vector3.up * (stepHeight + 0.1f);
            if (!Physics.Raycast(originHigh, transform.forward, stepRayLength, stepLayerMask))
            {
                // 위쪽은 비어있다 = 올라갈 수 있는 작은 턱
                // 살짝 위로 올려준다
                Debug.Log($"[StepClimb] 발동 위치={transform.position}, hit={hitLow.collider.name}");
                rb.position += Vector3.up * stepSmooth;
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            Anim.PlayWatering();
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            debugMode = !debugMode;

            if (playerStats != null)
                playerStats.DebugMode = debugMode;
            Debug.Log("디버그 모드 온오프");
        }
        CheckStorageDistance();

        if (isStunned == true)
        {
            stunTime += Time.deltaTime;

            if (stunTime >= stunDuration)
            { 
                isStunned = false;
                stunTime = 0f;
            }

            Anim.SetStun(isStunned);
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (robotPet != null)
            {
                robotPet.ToggleFlashlight();
            }
        }

        if (WaterTankService.I?.Manager != null)
        {
            WaterTankService.I.Manager.Tick(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        bool previousGround = isGround;

        // 1. CheckSphere로 기본 지면 체크
        isGround = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // 2. 점프 중이거나 확실히 떠있을 때만 Raycast 검증
        if (isGround && (wasJumping || Mathf.Abs(rb.linearVelocity.y) > 0.5f))
        {
            bool hasGroundBelow = Physics.Raycast(
                groundCheck.position,
                Vector3.down,
                out RaycastHit hit,
                groundCheckRadius + 0.3f,  // 거리 조금 늘림
                groundLayer
            );

            if (!hasGroundBelow)
            {
                isGround = false;
                Debug.Log("[벽 착지 방지] 아래 지면 없음");
            }
            else
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);

                if (angle > 55f)  // 각도 조금 완화
                {
                    isGround = false;
                    Debug.Log($"[벽 착지 방지] 가파른 경사 {angle:F1}도");
                }
            }
        }

        if (wasJumping && rb.linearVelocity.y > 0.1f)
        {
            isGround = false;
        }

        if (wasJumping && isGround)
        {
            Debug.Log("착지! Jump = false");
            if (Anim != null)
            {
                Anim.SetSlow(false);
                Anim.SetJumping(false);
            }
            wasJumping = false;
        }

        DropPlayer();
        MovePlayer();
        StepClimb();
        RotatePlayer();
        JumpPlayer();
        HandleBuildPlacementMode();

        wasGround = previousGround;
    }

    //private void FixedUpdate()
    //{
    //    bool previousGround = isGround;
    //    isGround = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

    //    //isGround = false;
    //    //if (Physics.Raycast(groundCheck.position, Vector3.down, out RaycastHit hit, groundCheckRadius + 0.1f, groundLayer))
    //    //{
    //    //    float angle = Vector3.Angle(hit.normal, Vector3.up);
    //    //    if (angle < 60f)
    //    //    {
    //    //        isGround = true;
    //    //    }
    //    //}

    //    if (wasJumping && rb.linearVelocity.y > 0.1f)
    //    {
    //        isGround = false;
    //    }

    //    if (wasJumping && isGround)
    //    {
    //        Debug.Log("착지! Jump = false");
    //        if (Anim != null)
    //        {
    //            Anim.SetSlow(false);
    //            Anim.SetJumping(false);
    //        }

    //        wasJumping = false;
    //    }


    //    DropPlayer();
    //    MovePlayer();
    //    StepClimb();
    //    RotatePlayer();
    //    JumpPlayer();
    //    HandleBuildPlacementMode();
    //    //GroundStick();

    //    wasGround = previousGround;
    //}

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnDeath -= HandleDeath;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.y < 0)
            isAutoRun = false;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isSprinting = true;
        }
        else if (context.canceled)
        {
            isSprinting = false;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGround)
            jumpRequested = true;

    }

    public void OnMoveSlow(InputAction.CallbackContext context)
    { 
        if(context.started)
            isSlow = true;

        if (context.canceled)
            isSlow = false;
    }

    public void OnFreeLook(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isFreeLook = true;
            thirdPersonCamera.EnterFreeLook();
        }
        if (context.canceled)
        {
            isFreeLook = false;
            thirdPersonCamera.ExitFreeLook();
        }
    }

    public void OnAutoRun(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isAutoRun = !isAutoRun;
        }
    }

    private void MovePlayer()
    {
        if (debugMode)
        {
            Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);

            Vector3 fly = cameraTransform.TransformDirection(move);

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                fly.y = 5f;
            }

            rb.linearVelocity = fly * debugSpeed;
            return;
        }
  
        if (!isGround || IsMovementLocked || isStunned) return;

        Vector2 finalMoveInput = moveInput;

        if (isAutoRun)
            finalMoveInput.y = 1f;

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 targetDirection = Vector3.zero;
        if (!isFreeLook)
        {
            targetDirection = (cameraRight * finalMoveInput.x + cameraForward * finalMoveInput.y).normalized;
        }
        else
        {
            //targetDirection = oldMoveDirection;
            Vector3 localForward = transform.forward;
            Vector3 localRight = transform.right;
            targetDirection = (localRight * finalMoveInput.x + localForward * finalMoveInput.y).normalized;
        }

        sprintSpeed = moveSpeed * 1.5f;

        if (finalMoveInput.magnitude < 0.1f)
        {
            targetSpeed = 0f;
            targetDirection = Vector3.zero;

            Anim.SetSlow(false);
        }
        else
        {
            if (!isSlow)
            {
                if (isAutoRun && isSprinting)
                    targetSpeed = sprintSpeed;
                else if (isSprinting)
                    targetSpeed = sprintSpeed;
                else
                    targetSpeed = moveSpeed;

                Anim.SetSlow(false);
            }
            else
            {
                targetSpeed = slowSpeed;
                Anim.SetSlow(true);
            }
        }


        moveDirection = Vector3.Slerp(moveDirection, targetDirection, turnSpeed * Time.deltaTime);
        //oldMoveDirection = moveDirection;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, changeSpeedRadius * Time.deltaTime);

        //float animationSpeed = currentSpeed;

        //if(targetSpeed > 0.1f && (isSlow || currentSpeed < moveSpeed))
        //{
        //    float minAnimSpeed = moveSpeed * minAnimationSpeedRatio;
        //    animationSpeed = Mathf.Max(currentSpeed, minAnimSpeed);
        //}

        //animator.SetFloat(speedAni, animationSpeed);

        Anim.SetSpeed(currentSpeed);

        Vector3 movement = moveDirection * currentSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }




    private void JumpPlayer()
    {
        if (debugMode) return;

        if (IsMovementLocked) { jumpRequested = false; return; }
        if (!jumpRequested || isStunned) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;

        Debug.Log("점프 실행! Jump = true");
        Anim.SetJumping(true);
        SoundManager.I?.StopBreathingHeavy();
        SoundManager.I?.PlayJump();

        jumpRequested = false;
        wasJumping = true;
    }

    private void RotatePlayer()
    {
        if (isStunned) return;

        if (isFreeLook)
        {
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 localRight = transform.right;
                Vector3 localForward = transform.forward;
                Vector3 inputDir = (localRight * moveInput.x + localForward * moveInput.y).normalized;

                if (inputDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(inputDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                }
            }
            return;
        }
        //bool isOnlyMovingBackward = moveInput.y < -0.1f && Mathf.Abs(moveInput.x) < 0.1f;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private async void HandleDeath()
    {
        isDead = true;
        isInteracting = false;
        rb.linearVelocity = Vector3.zero;
        UIManager.Instance.SetBlockingInput(true);
        UIManager.Instance.CloseAllUIs();

        Anim.SetToolType(0);
        Anim.HandLayerChange();

        var equipManager = GetComponent<PlayerEquipManager>();
        if (equipManager != null)
        {
            equipManager.ClearAllEquipData();
        }

        Anim.PlayDeath();
        Anim.SetRootMotion(true);

        Debug.Log("[PlayerController] 플레이어 기절!");

        await UniTask.Delay(4383);

        onPassOut?.Invoke();

        if (thirdPersonCamera != null)
        {
            originalZoomDistance = thirdPersonCamera.distance;
            thirdPersonCamera.enabled = false;
        }

        if (cinemachineBrain != null)
            cinemachineBrain.enabled = true;

        await UniTask.Delay(5000);

        if (cinemachineBrain != null)
            cinemachineBrain.enabled = false;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.distance = originalZoomDistance;
            thirdPersonCamera.enabled = true;
        }

        Anim.PlayerWakeUp();

        Anim.SetRootMotion(false);

        Anim.AnimatorRePosition();

        transform.position = spawnPoint.position;

        playerStats.GetResurrection();
        isDead = false;
    }


    private void DropPlayer()
    {
        if (debugMode) return;

        if (!isGround && wasGround)
        {
            highestY = transform.position.y;
        }
        else if (!isGround)
        {
            if (transform.position.y > highestY)
                highestY = transform.position.y;
        }
        else if (!wasGround && isGround)
        {
            float fallHeight = (highestY - transform.position.y) * transform.localScale.y;

            if (fallHeight > maxDropHeight)
                fallHeight = maxDropHeight;

            if (fallHeight > minDropHeight)
            {
                float effectiveHeight = fallHeight - minDropHeight;
                float shoeResist = playerEquip.GetHeightResist();

                float damage = Mathf.Pow(effectiveHeight, 1.8f) * shoeResist;

                if (playerStats != null)
                    playerStats.TakeDamage(damage);

                ApplyLandingSlow().Forget();
                Debug.Log($"낙하 높이: {fallHeight:F2}m, 데미지: {damage:F2}");
            }
        }
    }

    public void SetBlocked(bool blocked)
    {
        if (isInputBlocked == blocked) return;

        isInputBlocked = blocked;

        if (blocked)
        {
            // 입력/속도 초기화
            moveInput = Vector2.zero;
            targetSpeed = 0f;
            currentSpeed = 0f;
            isSprinting = false;
            isAutoRun  = false;
            jumpRequested = false;

            if (rb != null)
            {
                var v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(0f, v.y, 0f); // 수평 속도만 멈춤
            }

            Anim?.SetSpeed(0f);
        }
    }

    private async UniTask ApplyLandingSlow()
    {
        //float originalSpeed = moveSpeed;

        //moveSpeed *= 0.5f;

        //await UniTask.Delay((int)(landingSlow + 1000f));

        //moveSpeed = originalSpeed;

        object landingKey = new object();

        AddSpeedMultiplier(landingKey, 0.5f);

        await UniTask.Delay((int)(landingSlow * 1000f));

        RemoveSpeedMultiplier(landingKey);
    }

    private void HandleBuildPlacementMode()
    {
        if (BuildPlacementController.I == null) return;

        bool isBuildPlacing = BuildPlacementController.I.IsPlacing;

        if (isBuildPlacing && !wasBuildPlacing)
        {
            rotationSpeed = originalRotationSpeed * 0.3f;
            turnSpeed = originalTurnSpeed * 0.3f;
        }
        else if (!isBuildPlacing && wasBuildPlacing)
        {
            rotationSpeed = originalRotationSpeed;
            turnSpeed = originalTurnSpeed;
        }

        wasBuildPlacing = isBuildPlacing;
    }

    public void ApplyStun(float stunTime)
    {
        if (isStunned) return;

        isStunned = true;

        Debug.Log("[PlayerController] 플레이어 스턴");

        stunDuration = stunTime;
        this.stunTime = 0f;
    }

    private void GroundStick()
    {
        if (wasJumping || jumpRequested) return;
        if (moveInput.magnitude < 0.1f) return;
        if (rb.linearVelocity.y > 0.1f) return;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f, groundLayer))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle > 5f)
            {
                rb.AddForce(Vector3.down * 50f, ForceMode.Force);
            }
        }
    }

    public void SetMobileInput(Vector2 input, bool sprint)
    {
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
        {
            moveInput = input;
            isSprinting = sprint;
        }
    }

    public void RequestJump()
    {
        if (isGround && !IsMovementLocked && !isStunned)
        {
            jumpRequested = true;
        }
    }
}