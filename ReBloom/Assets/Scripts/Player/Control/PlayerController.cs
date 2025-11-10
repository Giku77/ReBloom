using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    private float sprintSpeed;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float slowSpeed = 4f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    
    private bool isSprinting = false;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private bool jumpRequested = false;
    private bool isSlow = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.Log("ī�޶� ����");
        }
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
        if (context.performed)
            jumpRequested = true;

    }

    public void OnMoveSlow(InputAction.CallbackContext context)
    { 
        if(context.performed)
            isSlow = true;

        if (context.canceled)
            isSlow = false;
    }

    private void MovePlayer()
    {
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        moveDirection = (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;
        sprintSpeed = moveSpeed * 1.5f;

        float currentSpeed;

        if (!isSlow)
        { currentSpeed = isSprinting ? sprintSpeed : moveSpeed; }

        else
        { currentSpeed = slowSpeed; }

            Vector3 movement = moveDirection * currentSpeed;

        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    private void JumpPlayer()
    {
        if (!jumpRequested) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;

        jumpRequested = false;
    }

    private void RotatePlayer()
    {
        bool isOnlyMovingBackward = moveInput.y < -0.1f && Mathf.Abs(moveInput.x) < 0.1f;
        
        if (moveDirection != Vector3.zero && !isOnlyMovingBackward)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}