using UnityEngine;

public class Rob11ColorManager : MonoBehaviour
{
    public Color[] predefinedColors = new Color[10];
    private Color rainbowColor = Color.red;

    public Renderer[] bodyRenderers;   
    public Renderer eyesRenderer;   
    public Renderer mouthRenderer;  
    public Renderer mouthSpeechRenderer;

    public bool isRainbowCycles=false;
    public bool isBattle=false;

    public int colorIndex = 0;
    public int eyesColorIndex = 1;
    public int mouthColorIndex = 2;

    [Range(0, 10)] public float emissionIntensity = 1f;

    void Start()
    {
        UpdateColors();
    }

    private void Update()
    {
        if (isRainbowCycles) 
        {
            float hue = Mathf.Repeat(Time.time / 2, 1f);
            rainbowColor = Color.HSVToRGB(hue, 1f, 1f);

            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    bodyRenderers[i].material.SetColor("_EmissionColor", rainbowColor * emissionIntensity);
                }
            }

                //bodyRenderer[i].material.SetColor("_EmissionColor", rainbowColor * emissionIntensity);
                mouthRenderer.material.SetColor("_EmissionColor", rainbowColor * emissionIntensity);
            eyesRenderer.material.SetColor("_EmissionColor", rainbowColor * emissionIntensity);
        }

    }
    private void UpdateColors()
    {
        ApplyColor(colorIndex);
    }

    private void ApplyColor( int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < predefinedColors.Length)
        {
            Color colorToApply = predefinedColors[colorIndex];

                    ApplyEmissionColor(colorToApply);
                
        }
    }

    private void ApplyEmissionColor( Color emissionColor)
    {
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
            {
                bodyRenderers[i].material.EnableKeyword("_EMISSION");
                bodyRenderers[i].material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            }
        }

        // bodyRenderer.material.EnableKeyword("_EMISSION");
        // bodyRenderer.material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        eyesRenderer.material.EnableKeyword("_EMISSION");
        eyesRenderer.material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        mouthRenderer.material.EnableKeyword("_EMISSION");
        mouthRenderer.material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        mouthSpeechRenderer.material.EnableKeyword("_EMISSION");
        mouthSpeechRenderer.material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
    }

    public void ChangeBodyColor(int newColorIndex)
    {
        if (newColorIndex >= 0 && newColorIndex < predefinedColors.Length)
        {
            colorIndex = newColorIndex;
            ApplyColor(colorIndex);
        }
    }


    public void ChangeEmissionIntensity(float newIntensity)
    {
        emissionIntensity = Mathf.Clamp(newIntensity, 0f, 10f); 
        UpdateColors(); 
    }
}
