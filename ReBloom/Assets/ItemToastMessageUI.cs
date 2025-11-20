using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemToastMessageUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject messageItemPrefab; // TextMeshProUGUI가 붙은 GameObject
    [SerializeField] private Transform messageContainer; // Vertical Layout Group

    // 아이콘 이미지
    [Header("Optional Icon Support")]
    [SerializeField] private bool useIcon = true;

    [Header("Settings")]
    private float messageDuration = 3f;
    private int maxMessageCount = 5;

    private Queue<GameObject> activeMessages = new Queue<GameObject>();

    private CancellationTokenSource _cts;

    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize(int maxCount, float duration)
    {
        maxMessageCount = maxCount;
        messageDuration = duration;

        if (messageItemPrefab == null)
        {
            Debug.LogError("[ItemToastMessageUI] messageItemPrefab이 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }

        if (messageContainer == null)
        {
            Debug.LogError("[ItemToastMessageUI] messageContainer가 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }

        _cts = new CancellationTokenSource();

        Debug.Log("[ItemToastMessageUI] 초기화 완료");
    }
    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        ClearAllMessages();
    }

    #region Public API
    /// <summary>
    /// 메시지 표시 (외부 호출용)
    /// </summary>
    public void DisplayMessage(string message, Sprite icon, Color textColor, float duration)
    {
        ShowMessage(message, icon, textColor, duration).Forget();
    }
    #endregion

    #region 메시지 표시
    /// <summary>
    /// 새 토스트 메시지 생성 및 표시
    /// </summary>
    private async UniTaskVoid ShowMessage(string message, Sprite icon, Color textColor, float duration)
    {
        // GameObject 생성
        GameObject messageObj = Instantiate(messageItemPrefab, messageContainer);

        // 텍스트 설정
        TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = textColor;
        }
        else
        {
            Debug.LogError("[ItemToastMessageUI] Prefab에 TextMeshProUGUI 컴포넌트가 없습니다!");
            Destroy(messageObj);
            return;
        }

        // 아이콘 설정 (옵션)
        if (useIcon && icon != null)
        {
            Image iconImage = messageObj.GetComponentInChildren<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
        }

        // 큐에 추가
        activeMessages.Enqueue(messageObj);

        // 최대 개수 초과 시 가장 오래된 메시지 제거
        if (activeMessages.Count > maxMessageCount)
        {
            RemoveOldestMessage();
        }

        Debug.Log($"[ItemToastMessageUI] 메시지 표시: {message}");

        await RemoveMessageAfterDelayAsync(messageObj, duration);
    }

    /// <summary>
    /// 일정 시간 후 메시지 제거 (UniTask 버전)
    /// </summary>
    private async UniTask RemoveMessageAfterDelayAsync(GameObject messageObj, float delay)
    {
        try
        {
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(delay),
                cancellationToken: _cts.Token
            );

            // 메시지가 아직 존재하는지 확인
            if (messageObj != null && activeMessages.Contains(messageObj))
            {
                RemoveMessage(messageObj);
            }
        }
        catch (System.OperationCanceledException)
        {
            // 취소됨 (OnDestroy 호출 시)
            Debug.Log($"[ItemToastMessageUI] 메시지 타이머 취소됨");
        }
    }

    /// <summary>
    /// 가장 오래된 메시지 제거
    /// </summary>
    private void RemoveOldestMessage()
    {
        if (activeMessages.Count > 0)
        {
            GameObject oldestMessage = activeMessages.Dequeue();
            if (oldestMessage != null)
            {
                Destroy(oldestMessage);
            }
        }
    }

    /// <summary>
    /// 특정 메시지 제거
    /// </summary>
    private void RemoveMessage(GameObject messageObj)
    {
        if (messageObj == null) return;

        // 큐에서 제거
        Queue<GameObject> tempQueue = new Queue<GameObject>();
        while (activeMessages.Count > 0)
        {
            GameObject msg = activeMessages.Dequeue();
            if (msg != messageObj)
            {
                tempQueue.Enqueue(msg);
            }
        }
        activeMessages = tempQueue;

        // GameObject 파괴
        Destroy(messageObj);
    }

    /// <summary>
    /// 모든 메시지 제거
    /// </summary>
    private void ClearAllMessages()
    {
        while (activeMessages.Count > 0)
        {
            GameObject msg = activeMessages.Dequeue();
            if (msg != null)
            {
                Destroy(msg);
            }
        }
    }
    #endregion
}