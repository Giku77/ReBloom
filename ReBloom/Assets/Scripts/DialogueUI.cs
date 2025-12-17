using Cysharp.Threading.Tasks;
using System.Threading;
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

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.25f; // 페이드 인/아웃 시간(초)

    private bool nextRequested;
    private CanvasGroup canvasGroup;


    private Color originalBgColor;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponent<CanvasGroup>();
        if (backgroundImage != null)
        {
            originalBgColor = backgroundImage.color;
        }
    }

    public void RequestNext()
    {
        nextRequested = true;
    }

    public async UniTask FadeAsync(float from, float to, float duration)
    {
        if (canvasGroup == null || duration <= 0f)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = to;
            return;
        }

        var token = this.GetCancellationTokenOnDestroy();

        float t = 0f;
        canvasGroup.alpha = from;

        while (t < duration)
        {
            if (token.IsCancellationRequested)
                return;

            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, lerp);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        canvasGroup.alpha = to;
    }


    public void OnNextInput(InputAction.CallbackContext ctx)
    {
        //if (!ctx.started) return;
        //Debug.Log("DialogueUI: OnNextInput received");
        nextRequested = true;
    }

    public override void Hide()
    {
        if (canvasGroup != null)
        {
            FadeAsync(canvasGroup.alpha, 0f, fadeDuration)
                .ContinueWith(() => base.Hide())
                .Forget();
        }
        else
        {
            base.Hide();
        }
    }

    public override void Show()
    {
        base.Show();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        // if (canvasGroup != null) {
        //     canvasGroup.alpha = 0f;
        //     FadeAsync(0f, 1f, fadeDuration).Forget();
        // }
    }

    public void HideInstant()
    {
        if (messageText != null)
            messageText.text = string.Empty;

        if (characterImage != null)
            characterImage.gameObject.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        base.Hide();
    }

    public async UniTask ShowLineAsync(
        string localizedText,
        bool showCharacterImage = false,
        bool waitForNextInput = true,
        bool showNextHint = true,
        Color textColor = new Color(),
        int alpha = 155,
        CancellationToken cancellationToken = default)
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

        var destroyToken = this.GetCancellationTokenOnDestroy();
        CancellationToken token = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(destroyToken, cancellationToken).Token
            : destroyToken;

        nextRequested = false;

        foreach (char ch in localizedText)
        {
            token.ThrowIfCancellationRequested();

            messageText.text += ch;
            SoundManager.I?.PlayTextBlip();

            if (nextRequested)
                break;

            await UniTask.Delay(
                (int)(1000f / typeSpeed),
                cancellationToken: token);
        }

        token.ThrowIfCancellationRequested();

        messageText.text = localizedText;

        if (!waitForNextInput)
        {
            nextRequested = false;
            return;
        }

        if (showNextHint)
        {
            messageText.text = localizedText +
                            " <color=#FFA500>[ENTER]</color>";
        }

        nextRequested = false;
        await UniTask.WaitUntil(() => nextRequested, cancellationToken: token);
        nextRequested = false;
    }
}
