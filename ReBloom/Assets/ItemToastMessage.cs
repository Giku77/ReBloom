using UnityEngine;

/// <summary>
/// 토스트 메시지 시스템 매니저
/// ItemBase를 직접 받아서 처리
/// </summary>
public class ItemToastMessage : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxMessageCount = 5;
    [SerializeField] private float messageDuration = 3f;
    //[SerializeField] private bool stackIdenticalMessages = true;

    [Header("References")]
    [SerializeField] private ItemToastMessageUI messageUI;

    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    private void Start()
    {
        // UI 자동 찾기
        if (messageUI == null)
        {
            messageUI = GetComponentInChildren<ItemToastMessageUI>();
            if (messageUI == null)
            {
                Debug.LogError("[ItemToastMessage] ItemToastMessageUI를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }

        // 데이터 검증
        if (inventoryData == null)
        {
            Debug.LogError("[ItemToastMessage] InventoryItemData가 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        // UI 초기화
        messageUI.Initialize(maxMessageCount, messageDuration);

        // ItemBase 기반 이벤트 구독
        inventoryData.OnItemToastMessage += OnItemAcquired;
        inventoryData.OnWarningMessage += OnWarningMessage;

        Debug.Log("[ItemToastMessage] 이벤트 구독 완료");
    }

    private void OnDestroy()
    {
        if (inventoryData != null)
        {
            inventoryData.OnItemToastMessage -= OnItemAcquired;
            inventoryData.OnWarningMessage -= OnWarningMessage;
        }
    }

    // ItemBase 처리 이벤트 핸들러
    private void OnItemAcquired(ItemBase item, int count)
    {
        if (item == null)
        {
            Debug.LogError("[ItemToastMessage] ItemBase가 null입니다!");
            return;
        }

        ShowItemMessage(item, count);
    }

    // 경고 메시지 핸들러
    private void OnWarningMessage(string message, Color color)
    {
        ShowWarningMessage(message, color);
    }

    #region Public API
    /// <summary>
    /// 간단한 텍스트 메시지 표시
    /// </summary>
    public void ShowMessage(string message)
    {
        ShowMessageInternal(message, null, Color.white);
    }

    /// <summary>
    /// ItemBase로 아이템 메시지 표시
    /// </summary>
    public void ShowItemMessage(ItemBase item, int count)
    {
        if (item == null)
        {
            Debug.LogError("[ItemToastMessage] ItemBase가 null입니다!");
            return;
        }

        string message = count > 1
            ? $"{item.itemName} +{count}"
            : $"{item.itemName} 획득";

        ShowMessageInternal(message, item.icon, Color.white);
    }

    /// <summary>
    /// 경고 메시지 표시
    /// </summary>
    public void ShowWarningMessage(string message, Color color)
    {
        ShowMessageInternal(message, null, color);
    }
    #endregion

    #region Internal Logic
    private void ShowMessageInternal(string message, Sprite icon, Color textColor)
    {
        if (messageUI == null)
        {
            Debug.LogError("[ItemToastMessage] UI가 초기화되지 않았습니다!");
            return;
        }

        messageUI.DisplayMessage(message, icon, textColor, messageDuration);
        Debug.Log($"[ItemToastMessage] 메시지 표시: {message}");
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Test Simple Message")]
    private void TestSimpleMessage()
    {
        ShowMessage("테스트 메시지입니다!");
    }

    [ContextMenu("Test Warning Message")]
    private void TestWarningMessage()
    {
        ShowWarningMessage("인벤토리가 가득 찼습니다!", Color.red);
    }
    #endregion
}