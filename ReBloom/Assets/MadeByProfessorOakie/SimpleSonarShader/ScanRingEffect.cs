using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class ScanRingEffect : MonoBehaviour
{
    [Header("Ring Settings")]
    [SerializeField] private float duration = 1.2f;   // 전체 재생 시간
    [SerializeField] private float maxRadius = 15f;    // 최종 반경 (Scale X/Z)
    [SerializeField]
    private AnimationCurve radiusCurve
        = AnimationCurve.Linear(0f, 0f, 1f, 1f);       // 0→1 선형
    [SerializeField]
    private AnimationCurve alphaCurve
        = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);    // 처음 진하게 → 서서히 0

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private Color _baseColor;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("[ScanRingEffect] Renderer not found.");
            enabled = false;
            return;
        }

        _mpb = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb);

        // 현재 머터리얼의 BaseColor를 기억해두기
        _baseColor = _renderer.sharedMaterial != null
            ? _renderer.sharedMaterial.GetColor(BaseColorID)
            : Color.cyan;
    }

    /// <summary>
    /// 링 재생 시작
    /// </summary>
    public void Play(Vector3 center, CancellationToken ct = default)
    {
        // Y 살짝 띄우고 싶으면 +0.05f 이런 식으로 보정
        transform.position = center;
        RunAsync(ct).Forget();
    }

    private async UniTaskVoid RunAsync(CancellationToken ct)
    {
        float t = 0f;
        float startY = transform.localScale.y;

        try
        {
            while (t < duration)
            {
                float n = t / duration; // 0~1

                // 반경 커지기
                float radius = radiusCurve.Evaluate(n) * maxRadius;
                transform.localScale = new Vector3(radius, startY, radius);

                // 알파 줄어들기
                float alpha = alphaCurve.Evaluate(n);

                _renderer.GetPropertyBlock(_mpb);
                Color c = _baseColor;
                c.a = alpha;
                _mpb.SetColor(BaseColorID, c);
                _renderer.SetPropertyBlock(_mpb);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                t += Time.deltaTime;
            }
        }
        catch (OperationCanceledException)
        {
            // 씬 전환/파괴 시 무시
        }

        Destroy(gameObject);
    }
}
