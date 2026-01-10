using TMPro;
using UnityEngine;

public class InteractionCanvasManager : MonoBehaviour
{
    public static InteractionCanvasManager Instance { get; private set; }

    [SerializeField] private Canvas sharedPromptCanvas;
    [SerializeField] private TextMeshProUGUI sharedPromptText;
    [SerializeField] private HoldInteractionUI holdInteractionUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HideAll();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void HideAll()
    {
        holdInteractionUI?.Hide();
        if (sharedPromptCanvas) sharedPromptCanvas.gameObject.SetActive(false);
    }

    public Canvas GetCanvas() => sharedPromptCanvas;
    public TextMeshProUGUI GetText() => sharedPromptText;
    public HoldInteractionUI GetHoldInteractionUI() => holdInteractionUI;
}
