using UnityEngine;

public class FarmSlotHighlight : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    private void Awake()
    {
        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<Renderer>();

        SetHighlighted(false);
    }

    public void SetHighlighted(bool on)
    {
        if (!targetRenderer) return;
        targetRenderer.enabled = on;
    }
}
