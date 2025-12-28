using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static UnityEngine.Splines.SplineInstantiate;

public class CutSceneManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;

    [SerializeField] private Image cutSceneImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup cutSceneGroup;
    [SerializeField] private GameObject skipHoldUI;

    [Header("Settings")]
    [SerializeField] private int introCutSceneId = 1;
    [SerializeField] private string seenKeyPrefix = "CutScene_";
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Defaults")]
    [SerializeField] private Sprite defaultCutSceneSprite;
    [SerializeField] private Sprite defaultBackgroundSprite;

    private CutSceneDB cutSceneDb;
    private CancellationTokenSource cutSceneCts;
    private bool isPlaying;
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        cutSceneDb = new CutSceneDB();
        cutSceneDb.LoadFromBG();

        if (skipHoldUI != null && PlatformManager.Instance != null &&
            PlatformManager.Instance.IsMobile)
        {
            skipHoldUI.GetComponentInChildren<TextMeshProUGUI>().text =
                "꾹 눌러서 스킵[터치]";
        }

        // if (cutSceneGroup != null)
        // {
        //     cutSceneGroup.alpha = 0f;
        //     cutSceneGroup.gameObject.SetActive(false);
        // }
    }

    private async void Start()
    {
        // await PlayCutSceneSequenceAsync(introCutSceneId);
    }

    public void SkipCutScene()
    {
        if (!isPlaying || cutSceneCts == null || cutSceneCts.IsCancellationRequested)
            return;

        Debug.Log("[CutScene] Skip requested.");
        cutSceneCts.Cancel();
    }

    private bool isFirst = true;
    private string currentImgName = "";
    public async UniTask PlayCutSceneSequenceAsync(int startCutSceneId)
    {
        if (isPlaying)
        {
            Debug.LogWarning("[CutScene] 이미 컷씬이 재생 중입니다.");
            return;
        }

        if (!cutSceneDb.TryGet(startCutSceneId, out var firstData))
            return;

        isPlaying = true;

        var destroyToken = this.GetCancellationTokenOnDestroy();
        cutSceneCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
        var token = cutSceneCts.Token;

        try
        {
            if (cutSceneGroup != null)
            {
                cutSceneGroup.gameObject.SetActive(true);
                cutSceneGroup.alpha = 0f;
            }

            await ApplyCutSceneVisualAsync(firstData);

            if (cutSceneGroup != null)
                await FadeCanvasGroupAsync(cutSceneGroup, 1f, fadeDuration, token);

            int currentId = startCutSceneId;

            while (!token.IsCancellationRequested &&
            cutSceneDb.TryGet(currentId, out var data))
            {
                bool imageChanged = currentImgName != data.ImageName;

                if (!isFirst)
                {
                    if (imageChanged)
                    {
                        if (dialogueUI != null)
                            dialogueUI.HideInstant();

                        VoiceManager.I?.Stop();


                        if (cutSceneGroup != null)
                            await FadeCanvasGroupAsync(cutSceneGroup, 0f, fadeDuration, token);
                    }
                }
                else
                {
                    isFirst = false;
                }

                await ApplyCutSceneVisualAsync(data);

                if (imageChanged && cutSceneGroup != null)
                    await FadeCanvasGroupAsync(cutSceneGroup, 1f, fadeDuration, token);

                currentImgName = data.ImageName;

                await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: token);

                Debug.Log($"[CutScene] VarcoID로 음성 재생 시도: {data.VarcoID}");
                if (data.VarcoID > 0)
                {
                    VoiceManager.I?.PlayVoice(data.VarcoID);
                }

                if (dialogueUI != null)
                    await dialogueUI.ShowLineAsync(data.TextKR, cancellationToken: token);

                if (token.IsCancellationRequested)
                    break;

                if (data.NextCutSceneID <= 0)
                    break;

                currentId = data.NextCutSceneID;
                SoundManager.I?.PlayCutSceneNext();
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[CutScene] 컷씬이 취소되었습니다 (스킵 또는 오브젝트 파괴).");
        }
        finally
        {
            VoiceManager.I?.Stop();

            if (cutSceneImage != null)
                cutSceneImage.gameObject.SetActive(false);

            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(false);

            if (skipHoldUI != null)
                skipHoldUI.SetActive(false);

            // if (cutSceneGroup != null)
            // {
            //     cutSceneGroup.alpha = 0f;
            //     cutSceneGroup.gameObject.SetActive(false);
            // }

            if (dialogueUI != null)
                dialogueUI.Hide();

            isPlaying = false;

            cutSceneCts?.Dispose();
            cutSceneCts = null;
        }
    }

    //디버그 모드에서 컷신 스킵하기 위하여 추가
    public void isDebugModeSkipCutScene()
    {
        if (cutSceneImage != null)
            cutSceneImage.gameObject.SetActive(false);

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        if (dialogueUI != null)
            dialogueUI.Hide();
    }

    private async UniTask ApplyCutSceneVisualAsync(CutSceneData data)
    {
        if (cutSceneImage != null)
        {
            Sprite sprite = await LoadSpriteSafeAsync(data.ImageName);
            cutSceneImage.sprite = sprite != null ? sprite : defaultCutSceneSprite;
            cutSceneImage.gameObject.SetActive(true);
        }

        if (backgroundImage != null)
        {
            if (backgroundImage.sprite == null)
                backgroundImage.sprite = defaultBackgroundSprite;

            backgroundImage.gameObject.SetActive(true);
        }
    }

    private async UniTask<Sprite> LoadSpriteSafeAsync(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return null;

        string key = $"CutScenes/{imageName}";
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(key);

        try
        {
            var sprite = await handle.Task;
            return sprite;
        }
        catch
        {
            Debug.LogWarning($"[CutScene] Addressables Sprite 로드 실패: {key}");
            return null;
        }
    }

    private async UniTask FadeCanvasGroupAsync(
        CanvasGroup group,
        float targetAlpha,
        float duration,
        CancellationToken token)
    {
        if (group == null || duration <= 0f)
        {
            if (group != null)
                group.alpha = targetAlpha;
            return;
        }

        float startAlpha = group.alpha;
        float time = 0f;

        while (time < duration)
        {
            token.ThrowIfCancellationRequested();

            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        group.alpha = targetAlpha;
    }
}
