using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintSpeed = 15f;
    [SerializeField] private float rotationSpeed = 1f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    
    private bool isSprinting = false;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.Log("Rigid Body 컴포넌트 없음");
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.Log("카메라 없음");
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer(); 
    }

    //인풋시스템 콜바이함수 무브
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    //인풋시스템 콜바이함수 스프린트
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

    //카메라 기준 플레이어 움직임 제어 함수
    private void MovePlayer()
    {
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        moveDirection = (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;
        
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 movement = moveDirection * currentSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    //카메라 기준 플레이어 회전 함수
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