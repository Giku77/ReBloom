using UnityEngine;
using UnityEngine.Video;

public class IntroLoopVideo : MonoBehaviour
{
    [SerializeField] private VideoPlayer vp;
    [SerializeField] private VideoClip introClip;
    [SerializeField] private VideoClip loopClip;
    [SerializeField] private bool playOnStart = true;

    [Header("Loop clip behavior")]
    [Tooltip("Loop clip: 첫 진입 때는 0초부터 재생")]
    [SerializeField] private bool loopClipFirstPlayFromZero = true;

    [Tooltip("Loop clip: 두 번째 반복부터 시작할 시간(초)")]
    [SerializeField] private double loopFromTime = 6.0;

    private bool switchedToLoop;
    private bool loopClipPlayedOnce;   

    private void Reset() => vp = GetComponent<VideoPlayer>();

    private void Awake()
    {
        if (!vp) vp = GetComponent<VideoPlayer>();

        vp.playOnAwake = false;
        vp.waitForFirstFrame = true;

        vp.isLooping = false; 
        vp.loopPointReached += OnClipFinished;
    }

    private void Start()
    {
        if (playOnStart) PlayIntro();
    }

    private void OnDestroy()
    {
        if (vp) vp.loopPointReached -= OnClipFinished;
    }

    public void PlayIntro()
    {
        switchedToLoop = false;
        loopClipPlayedOnce = false;

        vp.Stop();
        vp.isLooping = false;
        vp.clip = introClip;
        vp.time = 0;
        vp.Play();
    }

    private void OnClipFinished(VideoPlayer source)
    {
        if (!switchedToLoop && source.clip == introClip)
        {
            switchedToLoop = true;

            source.Stop();
            source.clip = loopClip;

            if (loopClipFirstPlayFromZero)
            {
                source.time = 0.0;
                loopClipPlayedOnce = true; 
            }
            else
            {
                source.time = loopFromTime;
                loopClipPlayedOnce = true; 
            }

            source.Play();
            return;
        }

        if (source.clip == loopClip)
        {
            source.Stop();

            source.time = loopFromTime;

            source.Play();
        }
    }
}
