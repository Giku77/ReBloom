using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [SerializeField] private AudioClip titleBGM;
    [SerializeField] private AudioClip[] mainBGMs;

    private AudioSource audioSource;
    private bool isPlayingMainBGM = false;

    private void Awake()
    {
        if (I == null)
        {
            I = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.volume = 0.5f;
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isPlayingMainBGM && !audioSource.isPlaying)
        {
            PlayRandomMainBGM();
        }
    }

    public void PlayTitleBGM()
    {
        isPlayingMainBGM = false;

        if (titleBGM == null)
        {
            Debug.LogWarning("[SoundManager] Title BGM이 할당되지 않았습니다!");
            return;
        }

        if (audioSource.clip != titleBGM)
        {
            audioSource.loop = true;
            audioSource.clip = titleBGM;
            audioSource.Play();
            Debug.Log($"[SoundManager] Title BGM 재생");
        }
    }

    public void PlayMainBGM()
    {
        if (mainBGMs == null || mainBGMs.Length == 0)
        {
            Debug.LogWarning("[SoundManager] Main BGM이 할당되지 않았습니다!");
            return;
        }

        isPlayingMainBGM = true;
        audioSource.loop = false;
        PlayRandomMainBGM();
    }

    private void PlayRandomMainBGM()
    {
        if (mainBGMs == null || mainBGMs.Length == 0) return;

        AudioClip randomClip = mainBGMs[Random.Range(0, mainBGMs.Length)];

        if (randomClip == null)
        {
            Debug.LogWarning("[SoundManager] 선택된 BGM이 null입니다!");
            return;
        }

        audioSource.clip = randomClip;
        audioSource.Play();
        Debug.Log($"[SoundManager] Main BGM 재생: {randomClip.name}");
    }

    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    public void StopBGM()
    {
        isPlayingMainBGM = false;
        audioSource.Stop();
    }
}