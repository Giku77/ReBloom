using UnityEngine;

public class CropVisual : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] renderers;

    [Header("Colors")]
    [SerializeField] private Color normalColor    = Color.white;
    [SerializeField] private Color wateredColor   = new Color(0.8f, 0.9f, 1f);
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.7f);
    [SerializeField] private Color witheredColor  = new Color(0.5f, 0.4f, 0.3f);

    [Header("Optional FX")]
    [SerializeField] private ParticleSystem waterFx;

    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        mpb = new MaterialPropertyBlock();

        ApplyColor(normalColor);
    }

    private void ApplyColor(Color c)
    {
        if (renderers == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();

        mpb.SetColor("_BaseColor", c); 

        foreach (var r in renderers)
        {
            if (!r) continue;          
            r.SetPropertyBlock(mpb);
        }
    }

    public void SetNormal()
    {
        ApplyColor(normalColor);
    }

    public void SetWatered()
    {
        ApplyColor(wateredColor);
        if (waterFx != null)
            waterFx.Play();
    }

    public void SetHighlighted(bool on)
    {
        ApplyColor(on ? highlightColor : normalColor);
    }

    public void SetWithered()
    {
        ApplyColor(witheredColor);
    }
}
