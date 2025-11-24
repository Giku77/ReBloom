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

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void SetSpeed(float speed)
    {
        animator.SetFloat(Speed, speed);
    }

    public void SetSlow(bool value)
    {
        animator.SetBool(Slow, value);
    }

    public void PlayJump()
    {
        animator.SetTrigger(Jump);
    }

    public void PlayDeath()
    {
        animator.SetTrigger(Death);
    }

    public void SetRootMotion(bool value)
    {
        animator.applyRootMotion = value;
    }

    public void PlayPickUp()
    {
        animator.SetTrigger(PickUp);
    }

    public void SetGathering(bool value)
    {
        animator.SetBool(Gather, value);
    }

    public void AnimatorRePosition()
    { 
        animator.transform.localPosition = Vector3.zero;
        animator.transform.localRotation = Quaternion.identity;
    }
}
