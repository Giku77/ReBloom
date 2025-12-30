using UnityEngine;

public class MechNPCSound : MonoBehaviour
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip footstep;
    [SerializeField] private AudioClip hit;
    [SerializeField] private AudioClip mechTransform;

    private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 25f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    public void PlayMechFootStep()
    {
        if (footstep != null)
            audioSource.PlayOneShot(footstep, 0.8f);
    }

    public void PlayHit()
    {
        if (hit != null)
            audioSource.PlayOneShot(hit);
    }

    public void PlayTransform()
    {
        if (mechTransform != null)
            audioSource.PlayOneShot(mechTransform);
    }

}
