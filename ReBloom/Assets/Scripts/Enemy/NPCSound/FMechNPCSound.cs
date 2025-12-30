using UnityEngine;

public class FMechNPCSound : MonoBehaviour
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip footstep;
    [SerializeField] private AudioClip laugh;

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
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    public void PlayFMechFootStep()
    {
        if (footstep != null)
            audioSource.PlayOneShot(footstep, 0.5f);
    }

    public void PlayLaugh()
    {
        if (laugh != null)
            audioSource.PlayOneShot(laugh);
    }
}
