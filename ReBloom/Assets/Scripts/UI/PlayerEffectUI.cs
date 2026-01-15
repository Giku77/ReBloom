using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEffectUI : UIBase
{
    private PlayerController player;

    [Header("기절 UI")]
    [SerializeField] private GameObject blurrObject;
    [SerializeField] private GameObject passOutLoadingScreen;
    [SerializeField] private RectTransform loadingImage;

    [Header("수면 페이드")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;

    protected override void Awake()
    {
        //player = GetComponent<PlayerController>();
        base.Awake();

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        blurrObject.SetActive(false);
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
        TryExistingBind();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        if (player != null)
            player.onPassOut -= PassOutUI;
    }

    private void BindLocalPlayer(GameObject localPlayer)
    {
        player = localPlayer.GetComponent<PlayerController>();
        if (player != null)
            player.onPassOut += PassOutUI;
    }

    private void TryExistingBind()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null) return;

        var po = nm.LocalClient?.PlayerObject;
        if (po == null) return;

        BindLocalPlayer(po.gameObject);
    }

    private void PassOutUI()
    { 
        ViewPassOutUI().Forget();
    }

    private async UniTask ViewPassOutUI()
    {
        blurrObject.SetActive(true);

        await UniTask.Delay(3000);

        blurrObject.SetActive(false);

        passOutLoadingScreen?.SetActive(true);
        //UIManager.Instance.ToggleUI(UIType.PlayerEffect);

        await MoveLoadingImage();

        //UIManager.Instance.ToggleUI(UIType.PlayerEffect);
        UIManager.Instance.SetBlockingInput(false);

        passOutLoadingScreen?.SetActive(false);
    }

    private async UniTask MoveLoadingImage()
    {
        Vector2 startPos = new Vector2(600f, loadingImage.anchoredPosition.y);
        Vector2 endPos = new Vector2(100f, loadingImage.anchoredPosition.y);

        loadingImage.anchoredPosition = startPos;

        float duration = 5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            loadingImage.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            await UniTask.Yield();
        }

        loadingImage.anchoredPosition = endPos;
    }

    public async UniTask FadeToBlack(float duration = -1f)
    {
        if (duration < 0) duration = fadeDuration;
        if (fadeImage == null) return;

        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Time.timeScale 영향 받지 않음
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            fadeImage.color = c;
            await UniTask.Yield();
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    public async UniTask FadeFromBlack(float duration = -1f)
    {
        if (duration < 0) duration = fadeDuration;
        if (fadeImage == null) return;

        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            fadeImage.color = c;
            await UniTask.Yield();
        }

        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(false);
    }
}
