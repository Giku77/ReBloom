using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip titleBGM;
    [SerializeField] private AudioClip[] mainBGMs;
    private AudioSource bgmSource;
    private bool isPlayingMainBGM = false;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private int sfxPoolSize = 10;
    private AudioSource[] sfxPool;
    private int currentSfxIndex = 0;

    [Header("플레이어 사운드")]
    public AudioClip jump;
    public AudioClip[] getDamageSounds;
    public AudioClip breathingHeavy;
    private AudioSource breathingHeavySource;

    [Header("UI 사운드")]
    public AudioClip openInventory;
    public AudioClip closeInventory;

    [Header("상호작용 사운드")]
    public AudioClip getWorldItem;
    public AudioClip build;
    public AudioClip crafting;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private bool shouldPlayBreathing = false;

    private void Awake()
    {
        if (I == null)
        {
            I = this;
            DontDestroyOnLoad(gameObject);

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = false;
            bgmSource.volume = 0.5f;
            bgmSource.playOnAwake = false;

            sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                sfxPool[i] = gameObject.AddComponent<AudioSource>();
                sfxPool[i].playOnAwake = false;
                sfxPool[i].volume = sfxVolume;
            }
        }
        else
        {
            Destroy(gameObject);
        }

        breathingHeavySource = gameObject.AddComponent<AudioSource>();
        breathingHeavySource.playOnAwake = false;
        breathingHeavySource.loop = true;
        breathingHeavySource.volume = 0.5f;
    }

    private void Update()
    {
        if (isPlayingMainBGM && !bgmSource.isPlaying)
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

        if (bgmSource.clip != titleBGM)
        {
            bgmSource.loop = true;
            bgmSource.clip = titleBGM;
            bgmSource.Play();
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
        bgmSource.loop = false;
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

        bgmSource.clip = randomClip;
        bgmSource.Play();
        Debug.Log($"[SoundManager] Main BGM 재생: {randomClip.name}");
    }

    public void SetVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }

    public void StopBGM()
    {
        isPlayingMainBGM = false;
        bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource source = sfxPool[currentSfxIndex];
        currentSfxIndex = (currentSfxIndex + 1) % sfxPoolSize;

        source.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (var source in sfxPool)
        {
            source.volume = sfxVolume;
        }
    }

    public void StartBreathingHeavy()
    {
        shouldPlayBreathing = true;

        if (breathingHeavy != null && !breathingHeavySource.isPlaying)
        {
            breathingHeavySource.clip = breathingHeavy;
            breathingHeavySource.Play();
        }
    }

    public void StopBreathingHeavy()
    {
        shouldPlayBreathing = false;

        breathingHeavySource.Stop();
    }

    public void PauseHeavyBreathing()
    {
        if (breathingHeavySource != null && breathingHeavySource.isPlaying)
        {
            breathingHeavySource.Pause();
        }
    }

    public void ResumeHeavyBreathing()
    {
        if (breathingHeavySource != null && shouldPlayBreathing && !breathingHeavySource.isPlaying)
        {
            breathingHeavySource.UnPause();
        }
    }

    public void PlayGetDamage()
    {
        if (getDamageSounds == null || getDamageSounds.Length == 0) return;

        AudioClip clip = getDamageSounds[Random.Range(0, getDamageSounds.Length)];
        PlaySFX(clip);
    }
    public void PlayJump() => PlaySFX(jump);
    public void PlayOpenInventory() => PlaySFX(openInventory);
    public void PlayCloseInventory() => PlaySFX(closeInventory);
    public void PlayGetWorldItem() => PlaySFX(getWorldItem);
    public void PlayBuild() => PlaySFX(build);
    public void PlayCrafting() => PlaySFX(crafting);

}