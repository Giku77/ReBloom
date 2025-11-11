using UnityEngine;

public class InteractionHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float backlightRange = 3f;
    [SerializeField] private float bodylightRange = 0.05f;

    private Light highlightLight;
    private bool isHighlighted = false;

    private Renderer highlightRend;

    private void Awake()
    {
        highlightLight = gameObject.AddComponent<Light>();
        highlightLight.type = LightType.Point;
        highlightLight.color = highlightColor;
        highlightLight.intensity = lightIntensity;
        highlightLight.range = backlightRange;
        highlightLight.enabled = false;

        highlightRend = GetComponent<Renderer>();
      }

    public void Show()
    {
        if (!isHighlighted)
        {
            highlightLight.enabled = true;
            isHighlighted = true;
        }

        if (highlightRend != null && highlightRend.material != null)
        {
            highlightRend.material.EnableKeyword("_EMISSION");
            highlightRend.material.SetColor("_EmissionColor", highlightColor * bodylightRange);
        }
    }

    public void Hide()
    {
        if (isHighlighted)
        {
            highlightLight.enabled = false;
            isHighlighted = false;
        }

        if (highlightRend != null && highlightRend.material != null)
        {
            highlightRend.material.DisableKeyword("_EMISSION");
            highlightRend.material.SetColor("_EmissionColor", Color.black);
        }
    }

    private void OnDestroy()
    {
        if (highlightLight != null)
        {
            Destroy(highlightLight);
        }
    }
}
