using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Slow = Animator.StringToHash("Slow");
    public static readonly int Death = Animator.StringToHash("Death");
    public static readonly int PickUp = Animator.StringToHash("PickUp");
    public static readonly int Gather = Animator.StringToHash("Gather");
    public static readonly int ToolType = Animator.StringToHash("ToolType");
    public static readonly int HitUpperBody = Animator.StringToHash("HitUpperBody");
    public static readonly int Jamming = Animator.StringToHash("Jamming");
    public static readonly int WakeUp = Animator.StringToHash("WakeUp");
    public static readonly int Sleep = Animator.StringToHash("Start");
    public static readonly int StandUp = Animator.StringToHash("StandUp");
    public static readonly int Stun = Animator.StringToHash("Stun");
    public static readonly int Watering = Animator.StringToHash("Watering");

    [Header("Layer Blending")]
    [SerializeField] private float layerBlendSpeed = 5f;
    private int toolLayerIndex = 1;
    private float targetLayerWeight = 0f;
    private bool isBlending = false;

    private bool isGathering = false;
    private Vector3 leftFootPosition;
    private Vector3 rightFootPosition;
    private Quaternion leftFootRotation;
    private Quaternion rightFootRotation;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        animator.SetLayerWeight(toolLayerIndex, 0f);
    }

    private void Update()
    {
        if (!isBlending) return;

        float currentWeight = animator.GetLayerWeight(toolLayerIndex);

        if (Mathf.Abs(currentWeight - targetLayerWeight) <= 0.01f)
        {
            animator.SetLayerWeight(toolLayerIndex, targetLayerWeight);
            isBlending = false;
            return;
        }

        float newWeight = Mathf.Lerp(
            currentWeight,
            targetLayerWeight,
            Time.deltaTime * layerBlendSpeed
        );
        animator.SetLayerWeight(toolLayerIndex, newWeight);
    }

    public void SetSpeed(float speed)
    {
        animator.SetFloat(Speed, speed);
    }

    public void SetSlow(bool value)
    {
        animator.SetBool(Slow, value);
    }

    public void SetJumping(bool value)
    {
        animator.SetBool(Jump, value);
    }

    public void SetStun(bool value)
    {
        animator.SetBool(Stun, value);
    }

    public void PlayDeath()
    {
        animator.SetTrigger(Death);
    }

    public void PlayWatering()
    {
        animator.SetTrigger(Watering);
    }

    public void SetRootMotion(bool value)
    {
        animator.applyRootMotion = value;
    }

    public void PlayPickUp()
    {
        animator.SetTrigger(PickUp);
    }

    public void PlaySleep()
    {
        animator.SetTrigger(Sleep);
    }

    public void PlayStandUp()
    {
        animator.SetTrigger(StandUp);
    }

    public void SetGathering(bool value)
    {
        animator.SetBool(Gather, value);
    }

    public void SetToolType(int toolType)
    {
        animator.SetInteger(ToolType, toolType);
    }

    public void SetHitAnim()
    {
        animator.SetTrigger(HitUpperBody);
    }

    public void SetJammingAnim()
    {
        animator.SetTrigger(Jamming);
    }

    public void PlayerWakeUp()
    {
        animator.SetTrigger(WakeUp);
    }

    public void AnimatorRePosition()
    {
        animator.transform.localPosition = Vector3.zero;
        animator.transform.localRotation = Quaternion.identity;
    }

    public void EquipToolLayerChange()
    {
        targetLayerWeight = 1f;
        isBlending = true;
    }

    public void HandLayerChange()
    {
        targetLayerWeight = 0f;
        isBlending = true;
    }
}