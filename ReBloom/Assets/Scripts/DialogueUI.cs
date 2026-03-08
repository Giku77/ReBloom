using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueUI : UIBase
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image poppiImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private float typeSpeed = 40f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Spawn Cover")]
    [SerializeField] private int spawnCoverHideDelayFrames = 2;

    private bool nextRequested;
    private CanvasGroup canvasGroup;
    private CancellationTokenSource visibilityCts;
    private Color originalBgColor;
    private bool isSpawnCoverVisible;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponent<CanvasGroup>();
        if (backgroundImage != null)
        {
            originalBgColor = backgroundImage.color;
            backgroundImage.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        UpdateSpawnCoverState();
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
    }

    private void HandleLocalPlayerSpawned(GameObject _)
    {
        HideSpawnCoverAfterFrames().Forget();
    }

    private void HandleLocalPlayerDespawned()
    {
        UpdateSpawnCoverState();
    }

    private void UpdateSpawnCoverState()
    {
        if (!ShouldShowSpawnCover())
        {
            HideSpawnCoverImmediate();
            return;
        }

        var nm = NetworkManager.Singleton;
        bool hasLocalPlayer = nm != null && nm.SpawnManager != null && nm.SpawnManager.GetLocalPlayerObject() != null;
        if (hasLocalPlayer)
        {
            HideSpawnCoverImmediate();
            return;
        }

        ShowSpawnCover();
    }

    private bool ShouldShowSpawnCover()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            return false;

        if (!nm.IsClient || nm.IsServer)
            return false;

        return SceneManager.GetActiveScene().name == "MainScene";
    }

    private void ShowSpawnCover()
    {
        if (backgroundImage == null)
            return;

        isSpawnCoverVisible = true;
        backgroundImage.gameObject.SetActive(true);
        backgroundImage.color = Color.black;
    }

    private void HideSpawnCoverImmediate()
    {
        isSpawnCoverVisible = false;

        if (backgroundImage == null)
            return;

        backgroundImage.color = originalBgColor;
        if (!IsOpen)
            backgroundImage.gameObject.SetActive(false);
    }

    private async UniTaskVoid HideSpawnCoverAfterFrames()
    {
        if (!isSpawnCoverVisible)
            return;

        var token = this.GetCancellationTokenOnDestroy();
        for (int i = 0; i < spawnCoverHideDelayFrames; i++)
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);

        HideSpawnCoverImmediate();
    }

    private void CancelVisibilityJob()
    {
        if (visibilityCts == null) return;
        visibilityCts.Cancel();
        visibilityCts.Dispose();
        visibilityCts = null;
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

    public async UniTask FadeAsync(float from, float to, float duration, CancellationToken token)
    {
        if (canvasGroup == null || duration <= 0f)
        {
            if (canvasGroup != null) canvasGroup.alpha = to;
            return;
        }

        float t = 0f;
        canvasGroup.alpha = from;

        while (t < duration)
        {
            token.ThrowIfCancellationRequested();

            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, lerp);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        canvasGroup.alpha = to;
    }

    public void OnNextInput(InputAction.CallbackContext ctx)
    {
        nextRequested = true;
    }

    public override void Hide()
    {
        CancelVisibilityJob();

        var destroyToken = this.GetCancellationTokenOnDestroy();
        visibilityCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
        var token = visibilityCts.Token;

        HideAsync(token).Forget();
    }

    private async UniTaskVoid HideAsync(CancellationToken token)
    {
        if (canvasGroup != null)
        {
            await FadeAsync(canvasGroup.alpha, 0f, fadeDuration, token);
            token.ThrowIfCancellationRequested();
        }

        base.Hide();
    }

    public override void Show()
    {
        CancelVisibilityJob();
        base.Show();

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private string GetNextHintTag()
    {
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
            return " <color=#FFA500>[ÅÍÄ¡]</color>";
        else
            return " <color=#FFA500>[ENTER]</color>";
    }

    public void HideInstant()
    {
        if (messageText != null)
            messageText.text = string.Empty;

        if (characterImage != null)
            characterImage.gameObject.SetActive(false);

        if (poppiImage != null)
            poppiImage.gameObject.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (backgroundImage != null && !isSpawnCoverVisible)
            backgroundImage.gameObject.SetActive(false);

        base.Hide();
    }

    public async UniTask ShowLineAsync(
        string localizedText,
        int varcoId = 0,
        bool showCharacterImage = false,
        bool showPoppiImage = false,
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
            backgroundImage.gameObject.SetActive(true);
            var c = originalBgColor;
            c.a = Mathf.Clamp01(alpha / 255f);
            backgroundImage.color = c;
            isSpawnCoverVisible = false;
        }

        messageText.text = "";
        Show();

        messageText.color = textColor;

        Debug.Log("DialogueUI: ShowLineAsync called with text: " + localizedText);
        await UniTask.DelayFrame(1);

        if (characterImage != null)
            characterImage.gameObject.SetActive(showCharacterImage);

        if (poppiImage != null)
            poppiImage.gameObject.SetActive(showPoppiImage);

        var destroyToken = this.GetCancellationTokenOnDestroy();
        CancellationToken token = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(destroyToken, cancellationToken).Token
            : destroyToken;

        nextRequested = false;

        bool useTextBlip = varcoId == 0;

        foreach (char ch in localizedText)
        {
            token.ThrowIfCancellationRequested();

            messageText.text += ch;

            if (useTextBlip)
                SoundManager.I?.PlayTextBlip();

            if (nextRequested)
                break;

            await UniTask.Delay((int)(1000f / typeSpeed), cancellationToken: token);
        }

        token.ThrowIfCancellationRequested();
        messageText.text = localizedText;

        if (!waitForNextInput)
        {
            nextRequested = false;
            return;
        }

        if (showNextHint)
            messageText.text = localizedText + GetNextHintTag();

        nextRequested = false;
        await UniTask.WaitUntil(() => nextRequested, cancellationToken: token);
        nextRequested = false;
    }

    protected override void OnHide()
    {
        if (backgroundImage != null && !isSpawnCoverVisible)
        {
            backgroundImage.color = originalBgColor;
            backgroundImage.gameObject.SetActive(false);
        }
    }
}

