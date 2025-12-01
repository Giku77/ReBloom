using UnityEngine;
using System.Collections;

/// <summary>
/// 로봇 애니메이션 재생을 관리하는 헬퍼 클래스
/// </summary>
public class RobotAnimationController : MonoBehaviour
{
    private Animator animator;

    [Header("애니메이션 설정")]
    [SerializeField] private float animationResetDelay = 2f;  // 애니메이션 후 리셋 대기 시간

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 애니메이션 재생 (자동으로 리셋됨)
    /// </summary>
    public void PlayAnimation(string animName, bool autoReset = true)
    {
        if (animator == null) return;

        // 애니메이션 트리거
        animator.SetBool(animName, true);

        // 자동 리셋
        if (autoReset)
        {
            StartCoroutine(ResetAnimationAfterDelay(animName, animationResetDelay));
        }
    }

    /// <summary>
    /// 애니메이션 파라미터 직접 설정
    /// </summary>
    public void SetAnimatorFloat(string paramName, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(paramName, value);
        }
    }

    public void SetAnimatorBool(string paramName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }

    public void SetAnimatorInt(string paramName, int value)
    {
        if (animator != null)
        {
            animator.SetInteger(paramName, value);
        }
    }

    /// <summary>
    /// 일정 시간 후 애니메이션 리셋
    /// </summary>
    private IEnumerator ResetAnimationAfterDelay(string animName, float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool(animName, false);
        animator.SetBool("reset", true);
        yield return new WaitForSeconds(0.1f);
        animator.SetBool("reset", false);
    }
}