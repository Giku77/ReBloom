using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ThunderSound : MonoBehaviour
{
    [Header("Thunder Sounds")]
    [SerializeField] private AudioClip[] thunderSounds;
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField] private bool spatialSound = false;

    [Header("Interval")]
    [SerializeField] private float minInterval = 3f;
    [SerializeField] private float maxInterval = 10f;



    private CancellationTokenSource cts;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialSound ? 1f : 0f;
    }

    private void OnEnable()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        PlayThunderLoopAsync(cts.Token).Forget();
    }

    private void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private async UniTask PlayThunderLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                float waitTime = Random.Range(minInterval, maxInterval);
                await UniTask.Delay((int)(waitTime * 1000), cancellationToken: token);

                float clipLength = PlayThunder();

                if (clipLength > 0)
                {
                    await UniTask.Delay((int)(clipLength * 1000), cancellationToken: token);
                }
            }
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    private float PlayThunder()
    {
        if (thunderSounds == null || thunderSounds.Length == 0)
        {
            return 0f;
        }

        AudioClip randomThunder = thunderSounds[Random.Range(0, thunderSounds.Length)];

        if (randomThunder != null)
        {
            audioSource.PlayOneShot(randomThunder, volume);
            return randomThunder.length;
        }

        return 0f;
    }
}