using UnityEngine;

public class AMechNPCSound : MonoBehaviour
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip footstep;
    [SerializeField] private AudioClip detectionBeep;

    private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 15f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

    public void PlayAMechFootStep()
    {
        if (footstep != null)
            audioSource.PlayOneShot(footstep, 0.5f);
    }

    public void PlayDetection()
    {
        if (detectionBeep != null)
            audioSource.PlayOneShot(detectionBeep);
    }
}
