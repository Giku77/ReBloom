using TMPro;
using UnityEngine;

public class InteractionHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float backlightRange = 3f;
    [SerializeField] private float bodylightRange = 0.05f;

    [Header("Prompt UI")]
    [SerializeField] private Canvas promptCanvas;              
    [SerializeField] private TextMeshProUGUI promptText;      
    public string promptFormat = "상호작용 [E]";

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

        // 처음에는 프롬프트 숨기기
        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // 프롬프트가 카메라를 바라보게 (빌보드)
        if (promptCanvas != null && promptCanvas.gameObject.activeSelf && Camera.main != null)
        {
            var cam = Camera.main.transform;
            promptCanvas.transform.rotation = Quaternion.LookRotation(
                promptCanvas.transform.position - cam.position, 
                Vector3.up
            );
        }
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

        if (promptCanvas != null)
        {
            if (promptText != null)
                promptText.text = promptFormat;

            promptCanvas.gameObject.SetActive(true);
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

        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(false);
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
