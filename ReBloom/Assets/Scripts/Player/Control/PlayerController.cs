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

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Jump Setting")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;

    private bool jumpRequested = false;

    private Animator animator;
    
    public static readonly string jumpAni = "Jump";
    public static readonly string speedAni = "Speed";


    bool isGround = false;

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
    }

    private void Start()
    {
        sprintSpeed = moveSpeed * 1.5f;
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
        JumpPlayer();
    }

    private void Update()
    {
        isGround = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
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

    private void MovePlayer()
    {
        if (!isGround) return;

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 targetDirection = Vector3.zero;
        if (!isFreeLook)
        {
            targetDirection = (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;
        }
        else
        {
            targetDirection = oldMoveDirection;
        }

        if (moveInput.magnitude < 0.1f)
        {
            targetSpeed = 0f;
            targetDirection = Vector3.zero;
        }
        else
        {
            if (!isSlow)
            {
                targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
            }
            else
            {
                targetSpeed = slowSpeed;
            }
        }

        moveDirection = Vector3.Slerp(moveDirection, targetDirection, turnSpeed * Time.deltaTime);
        oldMoveDirection = moveDirection;

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
            return;

        //bool isOnlyMovingBackward = moveInput.y < -0.1f && Mathf.Abs(moveInput.x) < 0.1f;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}