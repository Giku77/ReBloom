using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    private float sprintSpeed;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float turnSpeed = 15f;
    [SerializeField] private float slowSpeed = 4f;
    [SerializeField] private float changeSpeedRadius = 4;

    public float currentSpeed = 0f;
    private float targetSpeed;

    private bool isSprinting = false;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Vector3 oldMoveDirection;

    private bool isSlow = false;
    private bool isFreeLook = false;

    public bool isDead = false;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    //임시 장착 확인용 
    [Header("Equipment")]
    [SerializeField] private InventoryItemData inventoryItemData;
    public PlayerEquipManager playerEquip;

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

    private Animator animator;
    
    public static readonly string jumpAni = "Jump";
    public static readonly string speedAni = "Speed";

    bool isAutoRun = false;

    bool isGround = false;

    [Header("Debug")]
    [SerializeField] private float debugSpeed = 15f;
    private bool debugMode = false;

    public PlayerStats playerStats;

    private float highestY;
    private bool wasGround = false;

    [SerializeField] Transform spawnPoint;

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

        animator = GetComponentInChildren<Animator>();
        playerStats = GetComponent<PlayerStats>();
        playerEquip = GetComponent<PlayerEquipManager>();
    }

    private void Start()
    {
        sprintSpeed = moveSpeed * 1.5f;

        if (playerStats != null)
            playerStats.OnDeath += HandleDeath;
    }

    private void Update()
    {
        isGround = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        DropPlayer();

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            EquipWeapon();
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            debugMode = !debugMode;

            if (playerStats != null)
                playerStats.DebugMode = debugMode;
            Debug.Log("디버그 모드 온오프");
        }

        wasGround = isGround;
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
        JumpPlayer();
    }

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
        if(context.performed)
            isSlow = true;

        if (context.canceled)
            isSlow = false;
    }

    public void OnFreeLook(InputAction.CallbackContext context)
    {
        if (context.started)
            isFreeLook = true;

        if(context.canceled)
            isFreeLook = false;

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

            rb.linearVelocity = fly * debugSpeed;
            return;
        }

        if (!isGround) return;

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
            }
            else
            {
                targetSpeed = slowSpeed;
            }
        }


        moveDirection = Vector3.Slerp(moveDirection, targetDirection, turnSpeed * Time.deltaTime);
        //oldMoveDirection = moveDirection;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, changeSpeedRadius * Time.deltaTime);

        animator.SetFloat(speedAni, currentSpeed);

        Vector3 movement = moveDirection * currentSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    private void JumpPlayer()
    {
        if (!jumpRequested) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;

        if (animator != null)
        {
            animator.SetTrigger(jumpAni);
        }

        jumpRequested = false;
    }

    private void RotatePlayer()
    {
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

    //임시 장착 확인용
    private void EquipWeapon()
    {
        if (inventoryItemData == null || playerEquip == null)
        {
            Debug.LogError("[PlayerController] InventoryItemData 또는 PlayerEquipManager가 할당되지 않았습니다.");
            return;
        }
        
        int weaponItemId = 4302002;
        
        if (inventoryItemData.HasItem(weaponItemId, 1))
        {
            playerEquip.EquipItem(weaponItemId);
            Debug.Log($"[PlayerController] 무기 장착: {weaponItemId}");
        }
        else
        {
            Debug.LogWarning($"[PlayerController] 인벤토리에 아이템 {weaponItemId}이(가) 없습니다.");
        }
    }

    private void HandleDeath()
    {
        isDead = true;

        rb.linearVelocity = Vector3.zero;

        transform.position = spawnPoint.position;

        Debug.Log("Player is Dead!");

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

    private async UniTask ApplyLandingSlow()
    {
        float originalSpeed = moveSpeed;

        moveSpeed *= 0.5f;

        await UniTask.Delay((int)(landingSlow + 1000f));

        moveSpeed = originalSpeed;
    }
}