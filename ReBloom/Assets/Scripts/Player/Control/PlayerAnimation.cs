using UnityEngine;

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
    [SerializeField] private ServerAuthoritativeAnimBridge networkBridge;

    private int toolLayerIndex = 1;
    private float targetLayerWeight;
    private bool isBlending;

    public Animator Animator => animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (!networkBridge)
            networkBridge = GetComponent<ServerAuthoritativeAnimBridge>();
    }

    private void Start()
    {
        if (animator != null)
            animator.SetLayerWeight(toolLayerIndex, 0f);
    }

    private void Update()
    {
        if (!isBlending || animator == null)
            return;

        float currentWeight = animator.GetLayerWeight(toolLayerIndex);

        if (Mathf.Abs(currentWeight - targetLayerWeight) <= 0.01f)
        {
            animator.SetLayerWeight(toolLayerIndex, targetLayerWeight);
            isBlending = false;
            return;
        }

        float newWeight = Mathf.Lerp(currentWeight, targetLayerWeight, Time.deltaTime * layerBlendSpeed);
        animator.SetLayerWeight(toolLayerIndex, newWeight);
    }

    public void SetSpeed(float speed)
    {
        ApplyFloatLocal(Speed, speed);
    }

    public void SetSlow(bool value)
    {
        ApplyBoolLocal(Slow, value);
        networkBridge?.ReportBoolParam(Slow, value);
    }

    public void SetJumping(bool value)
    {
        ApplyBoolLocal(Jump, value);
        networkBridge?.ReportBoolParam(Jump, value);
    }

    public void SetStun(bool value)
    {
        ApplyBoolLocal(Stun, value);
        networkBridge?.ReportBoolParam(Stun, value);
    }

    public void PlayDeath()
    {
        ApplyTriggerLocal(Death);
        networkBridge?.ReportTriggerParam(Death);
    }

    public void PlayWatering()
    {
        ApplyTriggerLocal(Watering);
        networkBridge?.ReportTriggerParam(Watering);
    }

    public void SetRootMotion(bool value)
    {
        if (animator != null)
            animator.applyRootMotion = value;
    }

    public void PlayPickUp()
    {
        ApplyTriggerLocal(PickUp);
        networkBridge?.ReportTriggerParam(PickUp);
    }

    public void PlaySleep()
    {
        ApplyTriggerLocal(Sleep);
        networkBridge?.ReportTriggerParam(Sleep);
    }

    public void PlayStandUp()
    {
        ApplyTriggerLocal(StandUp);
        networkBridge?.ReportTriggerParam(StandUp);
    }

    public void SetGathering(bool value)
    {
        ApplyBoolLocal(Gather, value);
        networkBridge?.ReportBoolParam(Gather, value);
    }

    public void SetToolType(int toolType)
    {
        ApplyIntLocal(ToolType, toolType);
        networkBridge?.ReportIntParam(ToolType, toolType);
    }

    public void SetHitAnim()
    {
        ApplyTriggerLocal(HitUpperBody);
        networkBridge?.ReportTriggerParam(HitUpperBody);
    }

    public void SetJammingAnim()
    {
        ApplyTriggerLocal(Jamming);
        networkBridge?.ReportTriggerParam(Jamming);
    }

    public void PlayerWakeUp()
    {
        ApplyTriggerLocal(WakeUp);
        networkBridge?.ReportTriggerParam(WakeUp);
    }

    public void AnimatorRePosition()
    {
        if (animator == null)
            return;

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

    public void ApplyFloatLocal(int hash, float value)
    {
        animator?.SetFloat(hash, value);
    }

    public void ApplyBoolLocal(int hash, bool value)
    {
        animator?.SetBool(hash, value);
    }

    public void ApplyIntLocal(int hash, int value)
    {
        animator?.SetInteger(hash, value);
    }

    public void ApplyTriggerLocal(int hash)
    {
        if (animator == null)
            return;

        animator.SetTrigger(hash);
    }
}
