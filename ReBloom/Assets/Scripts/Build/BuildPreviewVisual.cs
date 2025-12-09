using UnityEngine;

public class BuildPreviewVisual : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;
    private Color editColor = Color.blue;

    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void SetValid(bool isValid)
    {
        Color c = isValid ? validColor : invalidColor;
        mpb.SetColor("_BaseColor", c);

        foreach (var r in renderers)
            r.SetPropertyBlock(mpb);
    }
    
    public void SetEditMode()
    {
        mpb.SetColor("_BaseColor", editColor);

        foreach (var r in renderers)
            r.SetPropertyBlock(mpb);
    }

    public void ResetColor()
    {
        mpb.Clear();
        foreach (var r in renderers)
        {
            r.SetPropertyBlock(null);
        }
    }
}
