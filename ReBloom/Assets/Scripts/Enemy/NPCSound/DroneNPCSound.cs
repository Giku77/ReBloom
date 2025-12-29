using UnityEngine;

public class DroneNPCSound : MonoBehaviour
{
    [SerializeField] private AudioClip laser;
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

    public void PlayLaser()
    {
        if (laser != null)
            audioSource.PlayOneShot(laser);
    }

    public void PlayDetection()
    {
        if (detectionBeep != null)
            audioSource.PlayOneShot(detectionBeep);
    }

}
