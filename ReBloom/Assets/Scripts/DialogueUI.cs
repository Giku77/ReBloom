using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueUI : UIBase  
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image characterImage;
    [SerializeField] private float typeSpeed = 40f; // 글자/초

    private bool nextRequested;
    private CanvasGroup canvasGroup;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponentInParent<CanvasGroup>();
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
        bool showNextHint = true)
    {
        messageText.text = "";
        Show();
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
