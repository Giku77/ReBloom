using UnityEngine;

public class BuildPreviewVisual : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;
    [SerializeField] private Color editColor = Color.yellow;

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
}
