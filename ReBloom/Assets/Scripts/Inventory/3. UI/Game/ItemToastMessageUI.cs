using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class ItemToastMessageUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject messageItemPrefab;
    [SerializeField] private Transform messageContainer;

    [Header("Settings")]
    [SerializeField] private int maxMessageCount = 5;
    [SerializeField] private float showDuration = 2.5f;

    // 아이콘 이미지
    [Header("Optional Icon Support")]
    [SerializeField] private bool useIcon = true;

    private Queue<GameObject> activeMessages = new Queue<GameObject>();

    public void Show(string message, Sprite icon)
    {
        GameObject obj = Instantiate(messageItemPrefab, messageContainer);
        activeMessages.Enqueue(obj);

        if (activeMessages.Count > maxMessageCount)
            RemoveOldest();

        SetupUI(obj, message, icon);
        PlayToastAnimation(obj);
    }
    public void ShowWarning(string message)
    {
        GameObject obj = Instantiate(messageItemPrefab, messageContainer);
        activeMessages.Enqueue(obj);

        if (activeMessages.Count > maxMessageCount)
            RemoveOldest();

        SetupUI(obj, message);
        PlayToastAnimation(obj);
    }

    private void SetupUI(GameObject obj, string message, Sprite icon = null)
    {
        var text = obj.GetComponentInChildren<TextMeshProUGUI>();
        var image = obj.GetComponentInChildren<Image>();

        text.text = message;

        if (image != null && icon != null)
        {
            image.sprite = icon;
            image.enabled = true;
        }
    }

    private void PlayToastAnimation(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();

        obj.transform.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        seq.Append(cg.DOFade(1f, 0.2f));
        seq.Join(obj.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));

        seq.AppendInterval(showDuration);

        seq.Append(cg.DOFade(0f, 0.2f));
        seq.Join(obj.transform.DOScale(0.8f, 0.2f));

        seq.OnComplete(() =>
        {
            Remove(obj);
        });
    }

    private void RemoveOldest()
    {
        if (activeMessages.Count == 0) return;

        GameObject old = activeMessages.Dequeue();
        if (old != null)
            Destroy(old);
    }

    private void Remove(GameObject obj)
    {
        if (activeMessages.Contains(obj))
        {
            var temp = new Queue<GameObject>();
            while (activeMessages.Count > 0)
            {
                var m = activeMessages.Dequeue();
                if (m != obj)
                    temp.Enqueue(m);
            }
            activeMessages = temp;
        }

        Destroy(obj);
    }
}