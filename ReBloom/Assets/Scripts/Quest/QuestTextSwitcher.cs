using DG.Tweening;
using TMPro;
using UnityEngine;

public class QuestTextSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject questText;      
    [SerializeField] private GameObject completeText;

    [Header("Animation Settings")]
    [SerializeField] private float slideDistance = 400f;  
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    private RectTransform questRect;
    private RectTransform completeRect;
    private Vector2 centerPos;    // 가운데 기준 위치

    private void Awake()
    {
        questRect = questText.GetComponent<RectTransform>();
        completeRect = completeText.GetComponent<RectTransform>();

        centerPos = questRect.anchoredPosition;

        completeRect.anchoredPosition = centerPos + Vector2.left * slideDistance;
        completeText.gameObject.SetActive(false);

        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
        {
            if (completeText != null)
            {
                completeText.GetComponentInChildren<TextMeshProUGUI>().text = completeText.GetComponentInChildren<TextMeshProUGUI>().text.Replace("[Tab]", "[터치]");
            }
        }
    }

    /// <summary>
    /// 퀘스트 완료 시 호출
    /// </summary>
    public void PlayQuestComplete()
    {
        completeText.gameObject.SetActive(true);

        questRect.DOKill();
        completeRect.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Join(
            questRect.DOAnchorPos(centerPos + Vector2.right * slideDistance, duration)
                .SetEase(ease)
        );

        seq.Join(
            completeRect.DOAnchorPos(centerPos, duration)
                .SetEase(ease)
        );

        seq.OnComplete(() =>
        {
            questText.gameObject.SetActive(false);
            questRect.anchoredPosition = centerPos;
        });
    }

    public void ResetQuestText()
    {
        questRect.DOKill();
        completeRect.DOKill();

        questText.gameObject.SetActive(true);
        questRect.anchoredPosition = centerPos;

        completeText.gameObject.SetActive(false);
        completeRect.anchoredPosition = centerPos + Vector2.left * slideDistance;
    }

    public bool IsAnimating()
    {
        return questText.gameObject.activeSelf == true;
    }
}
