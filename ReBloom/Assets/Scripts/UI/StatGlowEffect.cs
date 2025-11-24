using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class StatGlowEffect : MonoBehaviour
{
    [Header("Glow Images")]
    [SerializeField] private Image glowImage1;
    [SerializeField] private Image glowImage2;
    [SerializeField] private Image glowImage3;
    
    [Header("Fade Settings")]
    [SerializeField] private float minFadeDuration = 0.5f;
    [SerializeField] private float maxFadeDuration = 2f;
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 1f;
    
    private CancellationTokenSource cts;
    
    private void Start()
    {
        cts = new CancellationTokenSource();
        
        if (glowImage1 != null)
            GlowEffect(glowImage1, cts.Token).Forget();
        
        if (glowImage2 != null)
            GlowEffect(glowImage2, cts.Token).Forget();
        
        if (glowImage3 != null)
            GlowEffect(glowImage3, cts.Token).Forget();
    }
    
    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
    
    private async UniTaskVoid GlowEffect(Image glowImage, CancellationToken token)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(Random.Range(0f, 1f)), cancellationToken: token);
        
        while (!token.IsCancellationRequested)
        {
            float targetAlpha = Random.Range(minAlpha, maxAlpha);
            float duration = Random.Range(minFadeDuration, maxFadeDuration);
            
            await FadeTo(glowImage, targetAlpha, duration, token);
        }
    }
    
    private async UniTask FadeTo(Image glowImage, float targetAlpha, float duration, CancellationToken token)
    {
        Color color = glowImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (token.IsCancellationRequested) return;
            
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            glowImage.color = color;
            
            await UniTask.Yield(token);
        }
        
        color.a = targetAlpha;
        glowImage.color = color;
    }
}