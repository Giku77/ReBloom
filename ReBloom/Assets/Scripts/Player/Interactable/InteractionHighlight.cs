using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private HoldInteractionUI holdPromptUI;

    public HoldInteractionUI HoldPromptUI => holdPromptUI;
    public string promptFormat = "상호작용 [E]";

    private Light highlightLight;
    private bool isHighlighted = false;
    private Renderer highlightRend;

    public bool isPermanent = false;

    private void Awake()
    {
        highlightLight = gameObject.AddComponent<Light>();
        highlightLight.type = LightType.Point;
        highlightLight.color = highlightColor;
        highlightLight.intensity = lightIntensity;
        highlightLight.range = backlightRange;
        highlightLight.enabled = false;

        highlightRend = GetComponent<Renderer>();
        if (highlightRend == null)
            highlightRend = GetComponentInChildren<Renderer>();

        if (promptCanvas == null && InteractionCanvasManager.Instance != null)
        {
            promptCanvas = InteractionCanvasManager.Instance.GetCanvas();
            promptText = InteractionCanvasManager.Instance.GetText();
            holdPromptUI = InteractionCanvasManager.Instance.GetHoldInteractionUI();
        }
        else if (InteractionCanvasManager.Instance == null)
        {
            Debug.Log("InteractionCanvasManager.Instance가 null!");
        }

        // 처음에는 프롬프트 숨기기
        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(false);
        }
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
        ShowHighlightOnly();
        ShowPrompt();
    }

    /// <summary>빛/Emission만 켜기 (텍스트는 안 건드림)</summary>
    public void ShowHighlightOnly()
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

    /// <summary>프롬프트 텍스트만 켜기</summary>
    public void ShowPrompt(string overrideText = null)
    {
        if (promptCanvas == null) return;

        if (promptText != null)
            promptText.text = string.IsNullOrEmpty(overrideText) ? promptFormat : overrideText;

        promptCanvas.gameObject.SetActive(true);
    }

    /// <summary>프롬프트만 숨기기</summary>
    public void HidePrompt()
    {
        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(false);
    }

    public void Hide()
    {
        if (isPermanent) return;

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

        HidePrompt();
    }

    private void OnDestroy()
    {
        if (highlightLight != null)
        {
            Destroy(highlightLight);
        }
    }
}
