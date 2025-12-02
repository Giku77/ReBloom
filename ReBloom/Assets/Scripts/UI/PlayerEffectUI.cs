using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerEffectUI : MonoBehaviour
{
    private PlayerController player;

    [Header("기절 UI")]
    [SerializeField] private GameObject blurrObject;
    [SerializeField] private GameObject passOutLoadingScreen;
    [SerializeField] private RectTransform loadingImage;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }


    private void Start()
    {
        blurrObject.SetActive(false);
        passOutLoadingScreen.SetActive(false);
    }

    private void OnEnable()
    {
        player.onPassOut += PassOutUI;
    }

    private void OnDestroy()
    {
        player.onPassOut -= PassOutUI;
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

        passOutLoadingScreen.SetActive(true);

        await MoveLoadingImage();

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


}
