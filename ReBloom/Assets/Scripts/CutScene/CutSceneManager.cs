using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutSceneManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;

    [SerializeField] private Image cutSceneImage;      
    [SerializeField] private Image backgroundImage;    
    [SerializeField] private CanvasGroup cutSceneGroup; 

    [Header("Settings")]
    [SerializeField] private int introCutSceneId = 1;          
    [SerializeField] private string seenKeyPrefix = "CutScene_";
    [SerializeField] private float fadeDuration = 0.5f;        

    [Header("Defaults")]
    [SerializeField] private Sprite defaultCutSceneSprite;     
    [SerializeField] private Sprite defaultBackgroundSprite;   

    private CutSceneDB cutSceneDb;

    private void Awake()
    {
        cutSceneDb = new CutSceneDB();
        cutSceneDb.LoadFromBG();

        // if (cutSceneGroup != null)
        // {
        //     cutSceneGroup.alpha = 0f;
        //     cutSceneGroup.gameObject.SetActive(false);
        // }
    }

    private async void Start()
    {
        // if (!HasSeen(introCutSceneId))
        // {
        //     await PlayCutSceneSequenceAsync(introCutSceneId);
        //     MarkSeen(introCutSceneId);
        // }
        //await PlayCutSceneSequenceAsync(introCutSceneId);

    }

    // private bool HasSeen(int cutSceneId)
    // {
    //     return PlayerPrefs.GetInt(seenKeyPrefix + cutSceneId, 0) == 1;
    // }

    // private void MarkSeen(int cutSceneId)
    // {
    //     PlayerPrefs.SetInt(seenKeyPrefix + cutSceneId, 1);
    //     PlayerPrefs.Save();
    // }

    public async UniTask PlayCutSceneSequenceAsync(int startCutSceneId)
    {
        if (!cutSceneDb.TryGet(startCutSceneId, out var firstData))
            return;

        var token = this.GetCancellationTokenOnDestroy();

        if (cutSceneGroup != null)
        {
            cutSceneGroup.gameObject.SetActive(true);
            cutSceneGroup.alpha = 0f;
        }

        ApplyCutSceneVisual(firstData);

        if (cutSceneGroup != null)
            await FadeCanvasGroupAsync(cutSceneGroup, 1f, fadeDuration, token);

        int currentId = startCutSceneId;

        while (cutSceneDb.TryGet(currentId, out var data))
        {
            ApplyCutSceneVisual(data);

            await dialogueUI.ShowLineAsync(data.TextKR);

            if (data.NextCutSceneID <= 0)
                break;

            currentId = data.NextCutSceneID;
        }

        if (cutSceneGroup != null)
        {
            await FadeCanvasGroupAsync(cutSceneGroup, 0f, fadeDuration, token);
            cutSceneGroup.gameObject.SetActive(false);
        }

        // 마지막 컷신에서 UI 닫고 싶으면 여기에서
        // dialogueUI.Hide();
    }

    /// <summary>
    /// 컷신 데이터 기반으로 이미지/배경 세팅
    /// </summary>
    private void ApplyCutSceneVisual(CutSceneData data)
    {
        // cutSceneImage
        if (cutSceneImage != null)
        {
            Sprite sprite = LoadSpriteSafe(data.ImageName);
            cutSceneImage.sprite = sprite != null ? sprite : defaultCutSceneSprite;
        }

        // 배경은 필요하다면 CutSceneData에 필드를 추가해서 쓰거나,
        // 지금은 기본 배경만 깔아도 됨
        if (backgroundImage != null)
        {
          if (backgroundImage.sprite == null)
              backgroundImage.sprite = defaultBackgroundSprite;
          backgroundImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Resources / Addressables / SpriteAtlas 등 원하는 방식으로 교체해서 쓰면 됨
    /// </summary>
    private Sprite LoadSpriteSafe(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return null;

        return Resources.Load<Sprite>($"CutScenes/{imageName}");
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
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        group.alpha = targetAlpha;
    }
}
