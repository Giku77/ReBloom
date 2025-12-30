using UnityEngine;

public class MMechNPCSound : MonoBehaviour
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip footstep;
    [SerializeField] private AudioClip hit;

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

    public void PlayMMechFootStep()
    {
        if (footstep != null)
            audioSource.PlayOneShot(footstep, 0.5f);
    }

    public void PlayHit()
    {
        if (hit != null)
            audioSource.PlayOneShot(hit);
    }
}
