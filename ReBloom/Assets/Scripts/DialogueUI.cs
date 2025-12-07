using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueUI : UIBase  
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private float typeSpeed = 40f; // 글자/초

    private bool nextRequested;
    private CanvasGroup canvasGroup;


    private Color originalBgColor;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponentInParent<CanvasGroup>();
        if (backgroundImage != null)
        {
            originalBgColor = backgroundImage.color;
        }
    }

    public void OnNextInput(InputAction.CallbackContext ctx)
    {
        //if (!ctx.started) return;
        //Debug.Log("DialogueUI: OnNextInput received");
        nextRequested = true;
    }

    public override void Show()
    {
        base.Show();
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    public async UniTask ShowLineAsync(
        string localizedText,
        bool showCharacterImage = false,
        bool waitForNextInput = true,
        bool showNextHint = true,
        Color textColor = new Color(),
        int alpha = 98)
    {
        if (textColor == new Color())
            textColor = Color.white;
        if (backgroundImage != null)
        {
            var c = originalBgColor;
            c.a = Mathf.Clamp01(alpha / 255f);
            backgroundImage.color = c;
        }

        messageText.text = "";
        Show();

        messageText.color = textColor;

        Debug.Log("DialogueUI: ShowLineAsync called with text: " + localizedText);
        await UniTask.DelayFrame(1);

        if (characterImage != null)
            characterImage.gameObject.SetActive(showCharacterImage);

        var token = this.GetCancellationTokenOnDestroy();

        nextRequested = false;

        foreach (char c in localizedText)
        {
            messageText.text += c;

            if (nextRequested)
                break;

            await UniTask.Delay(
                (int)(1000f / typeSpeed),
                cancellationToken: token);
        }

        messageText.text = localizedText;

        if (!waitForNextInput)
        {
            nextRequested = false;
            return;
        }

        if (showNextHint)
        {
            messageText.text = localizedText +
                               " <color=#FFA500>[G] 다음</color>";
        }

        nextRequested = false;
        await UniTask.WaitUntil(() => nextRequested, cancellationToken: token);
        nextRequested = false;
    }
}
