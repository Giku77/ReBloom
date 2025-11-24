using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

public class RegionTitleUI : MonoBehaviour
{
    public static RegionTitleUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI txtMain;
    [SerializeField] private TextMeshProUGUI txtSub;
    [SerializeField] private AudioSource audioSource;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInTime = 0.4f;
    [SerializeField] private float fadeOutTime = 0.4f;
    [SerializeField] private float holdTimeDefault = 2f;
    //[SerializeField] private AnimationCurve scaleCurve;

    private CancellationTokenSource _animationCts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        canvasGroup.alpha = 0f;
        //panelRect.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;
    }

    public void ShowRegion(RegionDefinition region)
    {
        // 이전 애니메이션 취소
        _animationCts?.Cancel();
        _animationCts?.Dispose();

        _animationCts = new CancellationTokenSource();

        PlayAnimation(region, _animationCts.Token).Forget();
    }

    private async UniTask PlayAnimation(RegionDefinition region, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        // 텍스트/색상 세팅
        txtMain.text = region.displayName;
        txtSub.text = string.IsNullOrEmpty(region.subtitle) ? "" : region.subtitle;
        txtSub.gameObject.SetActive(!string.IsNullOrEmpty(region.subtitle));

        txtMain.color = region.mainColor;
        txtSub.color = region.mainColor * 0.9f;

        if (backgroundImage != null && region.backgroundSprite != null)
        {
            backgroundImage.sprite = region.backgroundSprite;
            backgroundImage.enabled = true;
        }

        if (region.enterSfx != null && audioSource != null)
        {
            audioSource.PlayOneShot(region.enterSfx);
        }

        // 시작 상태 (투명 + 살짝 축소)
        canvasGroup.alpha = 0f;
        //panelRect.localScale = Vector3.one * 0.8f;

        // ===== 페이드 인 + 스케일 =====
        float t = 0f;
        while (t < fadeInTime)
        {
            if (token.IsCancellationRequested) return;

            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeInTime);

            canvasGroup.alpha = normalized;

            //float scaleEval = scaleCurve != null ? scaleCurve.Evaluate(normalized) : normalized;
            //float scale = Mathf.Lerp(0.8f, 1.05f, scaleEval);
            //panelRect.localScale = Vector3.one * scale;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        //panelRect.localScale = Vector3.one;

        // ===== 유지 시간 =====
        float holdTime = region.showDuration > 0 ? region.showDuration : holdTimeDefault;
        if (holdTime > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(holdTime), cancellationToken: token);
        }

        // ===== 페이드 아웃 =====
        t = 0f;
        while (t < fadeOutTime)
        {
            if (token.IsCancellationRequested) return;

            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeOutTime);

            canvasGroup.alpha = 1f - normalized;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        canvasGroup.alpha = 0f;
        //panelRect.localScale = Vector3.one;
    }
}
