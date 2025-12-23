using UnityEngine;

public class FootStep : MonoBehaviour
{
    private PlayerAnimation anim;
    private bool isIndoor = false;

    private void Awake()
    {
        anim = GetComponentInParent<PlayerAnimation>();
    }

    private void OnEnable()
    {
        StageDetector.OnEnterDoor += OnEnterDoor;
    }

    private void OnDisable()
    {
        StageDetector.OnEnterDoor -= OnEnterDoor;
    }

    //public void PlayFootStep(float volumeScale)
    //{
    //    SoundManager.I?.PlayFootStep(volumeScale);
    //}

    private void OnEnterDoor(bool insideDoor)
    {
        isIndoor = insideDoor;
        Debug.Log($"[FootStep] 실내 여부: {isIndoor}");
    }


    public void PlayFootStep()
    {
        if (anim?.Animator == null) return;

        float speed = anim.Animator.GetFloat("Speed");

        float t = Mathf.Clamp01(speed / 10f);
        float volumeScale = Mathf.Lerp(0.1f, 0.4f, t* t);

        Debug.Log($"[FootStep] Speed: {speed}, VolumeScale: {volumeScale}");

        SoundManager.I?.PlayFootStep(volumeScale, isIndoor);
    }
}
