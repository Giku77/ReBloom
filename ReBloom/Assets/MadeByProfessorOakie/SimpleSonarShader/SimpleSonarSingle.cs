using UnityEngine;

public class SimpleSonarSingle : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;  // Scan 실린더 렌더러
    [SerializeField] private float defaultIntensity = 15f;

    private Material _mat;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null)
            _mat = targetRenderer.material; // 인스턴스
    }

    public void StartSonarRing(Vector3 worldPos, float intensity = -1f)
    {
        if (_mat == null) return;

        if (intensity <= 0f)
            intensity = defaultIntensity;

        var data = new Vector4(worldPos.x, worldPos.y, worldPos.z, Time.time);
        _mat.SetVector("_RingOriginAndStartT", data);
        _mat.SetFloat("_RingIntensity", intensity);
    }
}
