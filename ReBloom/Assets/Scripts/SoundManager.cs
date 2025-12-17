using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("BGM")]
    [SerializeField] private AssetReferenceT<AudioClip> titleBGM;
    [SerializeField] private AssetReferenceT<AudioClip>[] mainBGMs;
    private AudioSource bgmSource;
    private bool isPlayingMainBGM = false;
    private AsyncOperationHandle<AudioClip>? currentBGMHandle;
    private bool isLoadingBGM = false;

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
    public AudioClip uiClick;
    public AudioClip error;
    public AudioClip cutSceneNext;
    public AudioClip textBlip;
    public AudioClip missionClear;
    public AudioClip nextMission;

    [Header("상호작용 사운드")]
    public AudioClip getWorldItem;
    public AudioClip build;
    public AudioClip crafting;
    public AudioClip openCraftingTable;
    public AudioClip closeCraftingTable;
    private AudioSource gatherSource;
    public AudioClip gatherHand;
    public AudioClip gatherShovel;
    public AudioClip gatherHammer;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private bool shouldPlayBreathing = false;

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = false;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxPool[i] = gameObject.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
            sfxPool[i].volume = sfxVolume;
        }

        breathingHeavySource = gameObject.AddComponent<AudioSource>();
        breathingHeavySource.playOnAwake = false;
        breathingHeavySource.loop = true;
        breathingHeavySource.volume = 0.5f;

        gatherSource = gameObject.AddComponent<AudioSource>();
        gatherSource.playOnAwake = false;
        gatherSource.loop = true;
        gatherSource.volume = sfxVolume;
    }

    private void Update()
    {
        if (isPlayingMainBGM && !bgmSource.isPlaying && !isLoadingBGM)
        {
            PlayRandomMainBGMAsync().Forget();
        }
    }

    private void OnDestroy()
    {
        ReleaseCurrentBGM();
    }

    //private void PlayBGM(AssetReferenceT<AudioClip> bgmRef, bool loop)
    //{
    //    if (bgmRef == null) return;

    //    ReleaseCurrentBGM();

    //    bgmSource.loop = loop;

    //    currentBGMHandle = bgmRef.LoadAssetAsync();
    //    currentBGMHandle.Value.Completed += handle =>
    //    {
    //        if (handle.Status != AsyncOperationStatus.Succeeded) return;

    //        bgmSource.clip = handle.Result;
    //        bgmSource.Play();
    //    };
    //}

    private void ReleaseCurrentBGM()
    {
        if (currentBGMHandle.HasValue && currentBGMHandle.Value.IsValid())
        {
            Addressables.Release(currentBGMHandle.Value);
            currentBGMHandle = null;
        }
    }

    public void PlayTitleBGM()
    {
        isPlayingMainBGM = false;
        PlayBGMAsync(titleBGM, true).Forget();
    }

    public void PlayMainBGM()
    {
        if (mainBGMs == null || mainBGMs.Length == 0) return;

        isPlayingMainBGM = true;
        bgmSource.loop = false;
        PlayRandomMainBGMAsync().Forget();
    }

    private async UniTask PlayBGMAsync(AssetReferenceT<AudioClip> bgmRef, bool loop)
    {
        if (bgmRef == null || isLoadingBGM) return;

        isLoadingBGM = true;
        ReleaseCurrentBGM();

        currentBGMHandle = bgmRef.LoadAssetAsync();
        await currentBGMHandle.Value.ToUniTask();

        if (currentBGMHandle.HasValue && currentBGMHandle.Value.Status == AsyncOperationStatus.Succeeded)
        {
            bgmSource.clip = currentBGMHandle.Value.Result;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        isLoadingBGM = false;
    }

    private async UniTask PlayRandomMainBGMAsync()
    {
        if (mainBGMs == null || mainBGMs.Length == 0 || isLoadingBGM) return;

        int index = Random.Range(0, mainBGMs.Length);
        await PlayBGMAsync(mainBGMs[index], false);
    }

    public void StopBGM()
    {
        isPlayingMainBGM = false;
        bgmSource.Stop();
        ReleaseCurrentBGM();
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

        gatherSource.volume = sfxVolume;
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

    public void PlayGather(int toolType)
    {
        if (gatherSource.isPlaying) return;

        gatherSource.loop = true;

        switch (toolType)
        {
            case 0: gatherSource.clip = gatherHand; break;
            case 1: gatherSource.clip = gatherShovel; break;
            case 2: gatherSource.clip = gatherHammer; break;
        }

        gatherSource.Play();
    }

    public void StopGather()
    {
        if (!gatherSource.isPlaying) return;
        gatherSource.Stop();
        gatherSource.clip = null;
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
    public void PlayOpenCraftingTable() => PlaySFX(openCraftingTable);
    public void PlayCloseCraftingTable() => PlaySFX(closeCraftingTable);
    public void PlayUIClick() => PlaySFX(uiClick);
    public void PlayError() => PlaySFX(error);
    public void PlayCutSceneNext() => PlaySFX(cutSceneNext);
    public void PlayTextBlip() => PlaySFX(textBlip);
    public void PlayNextMission() => PlaySFX(nextMission);
    public void PlayMissionClear() => PlaySFX(missionClear);
    public void PlayGatherHand() => PlaySFX(gatherHand);
    public void PlayGatherShovel() => PlaySFX(gatherShovel);
    public void PlayGatherHammer() => PlaySFX(gatherHammer);
}