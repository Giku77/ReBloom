using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class ToastMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;

    private CancellationTokenSource showCts;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
    }

    public void Show(string message, float duration = 2f)
    {
        ShowInternal(message, duration);
    }

    private void OnDestroy()
    {
        showCts?.Cancel();
        showCts?.Dispose();
    }

    private void ShowInternal(string message, float duration)
    {
        showCts?.Cancel();
        showCts?.Dispose();
        showCts = new CancellationTokenSource();

        ShowRoutineAsync(message, duration, showCts.Token).Forget();
    }

    private async UniTask ShowRoutineAsync(string message, float duration, CancellationToken token)
    {
        messageText.text = message;

        // 페이드 인
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            // 한 프레임 대기
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        canvasGroup.alpha = 1f;

        // duration 만큼 대기
        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);

        // 페이드 아웃
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        canvasGroup.alpha = 0f;
    }
}
