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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("InteractionCanvasManager가 중복 생성되었습니다!");
        }
    }

    public Canvas GetCanvas() => sharedPromptCanvas;
    public TextMeshProUGUI GetText() => sharedPromptText;
    public HoldInteractionUI GetHoldInteractionUI() => holdInteractionUI;
}
