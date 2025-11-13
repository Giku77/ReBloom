using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameInventory : MonoBehaviour, IInventoryProvider
{
    [Header("UI References")]
    public TextMeshProUGUI message;
    public QuickSlot quickSlot;
    public GameObject InventoryUIRoot;
    [SerializeField] private List<TextMeshProUGUI> itemsTextInfo;

    [Header("Inventory Settings")]
    [SerializeField] private int maxInventorySlots = 10;  // 최대 슬롯 개수

    private readonly Dictionary<int, int> _items = new()
    {
        // 테스트용 미리 제공되는 아이템 리스트
        { 4102001, 15 },
        { 4102002, 6 },
        { 4102005, 10 },
    };

    public Dictionary<int, int> Items => _items;

    #region IInventoryProvider 구현
    public int GetItemCount(int itemId)
    {
        return _items.TryGetValue(itemId, out var cnt) ? cnt : 0;
    }

    /// <summary>
    /// 아이템 추가 (최대 슬롯 체크 포함)
    /// </summary>
    public void AddItem(int itemId, int amount)
    {
        // 이미 보유중인 아이템이면 수량만 증가
        if (_items.ContainsKey(itemId))
        {
            _items[itemId] += amount;
            ShowMessage($"{ItemDatabase.I.GetItem(itemId).name}을(를) {amount}개 획득했습니다.");
        }
        else
        {
            // 새 아이템 추가 시 슬롯 개수 체크
            if (_items.Count >= maxInventorySlots)
            {
                ShowMessage($"최대 개수({maxInventorySlots}개)에 도달하여 획득 실패!");
                Debug.LogWarning($"[인벤토리] 슬롯이 가득 참! ({_items.Count}/{maxInventorySlots})");
                return;
            }

            _items[itemId] = amount;
            ShowMessage($"{ItemDatabase.I.GetItem(itemId).name}을(를) {amount}개 획득했습니다.");
        }

        // UI 갱신
        UpdateInventoryUI();
    }

    public void RemoveItem(int itemId, int amount)
    {
        if (_items.ContainsKey(itemId))
        {
            _items[itemId] -= amount;

            // 수량이 0 이하면 아이템 제거
            if (_items[itemId] <= 0)
            {
                _items.Remove(itemId);
            }

            // UI 갱신
            UpdateInventoryUI();
        }
    }

    public void Clear()
    {
        _items.Clear();
        UpdateInventoryUI();
    }

    public bool HasItem(int itemId, int amount)
    {
        return GetItemCount(itemId) >= amount;
    }

    public void Consume(int itemId, int amount)
    {
        if (ItemDatabase.I.GetItem(itemId).canUseable)
        {
            RemoveItem(itemId, amount);
            ShowMessage($"{ItemDatabase.I.GetItem(itemId).name}을(를) {amount}개 사용했습니다.");
        }
        else
        {
            ShowMessage($"{ItemDatabase.I.GetItem(itemId).name}은(는) 사용할 수 없습니다.");
        }
    }
    #endregion

    #region 인벤토리 UI 갱신
    /// <summary>
    /// 인벤토리 UI 텍스트 업데이트
    /// 형식: "아이템 이름: / 수량: 0개"
    /// </summary>
    public void UpdateInventoryUI()
    {
        Debug.Log($"[UpdateInventoryUI]{itemsTextInfo.Count}");
        if (itemsTextInfo == null || itemsTextInfo.Count == 0)
        {
            Debug.LogWarning("[인벤토리] itemsTextInfo가 비어있습니다!");
        //    Debug.LogWarning(
        //    $"[인벤토리] itemsTextInfo가 비어있습니다! (obj={name}, count={itemsTextInfo?.Count ?? -1})",
        //    this // <- 로그 클릭하면 해당 GameObject 하이라키에서 선택됨
        //);
            return;
        }

        int index = 0;

        // 보유 아이템 표시
        foreach (var itemPair in _items)
        {
            if (index >= itemsTextInfo.Count) break;

            int itemId = itemPair.Key;
            int quantity = itemPair.Value;

            ItemBase item = ItemDatabase.I.GetItem(itemId);

            if (item != null)
            {
                // 텍스트 형식: "아이템 이름: / 수량: 15개"
                itemsTextInfo[index].text = $"{item.name} / 수량: {quantity}개";
            }
            else
            {
                itemsTextInfo[index].text = $"알 수 없는 아이템({itemId}) / 수량: {quantity}개";
            }

            index++;
        }

        // 나머지 빈 슬롯 표시
        for (int i = index; i < itemsTextInfo.Count; i++)
        {
            itemsTextInfo[i].text = "빈 슬롯";
            itemsTextInfo[i].color = Color.gray;
        }

        //Debug.Log($"[인벤토리] UI 업데이트 완료 ({_items.Count}/{maxInventorySlots} 슬롯 사용중)");
    }

    /// <summary>
    /// 메시지 표시
    /// </summary>
    private void ShowMessage(string msg)
    {
        if (message != null)
        {
            message.text = msg;
            Debug.Log($"[인벤토리 메시지] {msg}");
        }
    }
    #endregion

    #region 퀵슬롯 관련
    /// <summary>
    /// 개별 아이템을 퀵슬롯에 배치 시도
    /// </summary>
    public void TryAssignQuickSlot(int itemId)
    {
        if (!_items.ContainsKey(itemId))
        {
            ShowMessage($"인벤토리에 해당 아이템이 없습니다.");
            return;
        }

        if (CanAssignQuickSlot(itemId))
        {
            AssignQuickSlot(ItemDatabase.I.GetItem(itemId), GetItemCount(itemId));
            ShowMessage($"{ItemDatabase.I.GetItem(itemId).name}을(를) 퀵슬롯에 배치했습니다.");
        }
        else
        {
            ShowMessage($"{ItemDatabase.I.GetItem(itemId).name}은(는) 퀵슬롯에 배치할 수 없습니다.");
        }
    }

    /// <summary>
    /// 인벤토리의 모든 아이템을 순차적으로 퀵슬롯에 배치 시도 (O키용)
    /// </summary>
    public int AutoFillQuickSlots()
    {
        int filledCount = 0;

        // 인벤토리의 모든 아이템을 순회
        foreach (var itemPair in _items)
        {
            int itemId = itemPair.Key;
            int quantity = itemPair.Value;

            // 퀵슬롯에 배치 가능한지 확인
            if (CanAssignQuickSlot(itemId))
            {
                ItemBase item = ItemDatabase.I.GetItem(itemId);

                // 퀵슬롯에 배치 시도
                if (quickSlot.TryAssign(item, quantity))
                {
                    filledCount++;
                    Debug.Log($"[퀵슬롯] {item.name} x{quantity} 배치 성공");
                }
                else
                {
                    // 슬롯이 꽉 찼으면 종료
                    ShowMessage($"퀵슬롯이 가득 참 (총 {filledCount}개 배치)");
                    Debug.Log($"[퀵슬롯] 슬롯이 가득 참 (총 {filledCount}개 배치)");
                    break;
                }
            }
        }

        if (filledCount > 0)
        {
            ShowMessage($"퀵슬롯에 {filledCount}개 아이템 배치 완료!");
        }
        else
        {
            ShowMessage("퀵슬롯에 배치할 수 있는 아이템이 없습니다.");
        }

        return filledCount;
    }

    public void AssignQuickSlot(ItemBase item, int quantity)
    {
        quickSlot.TryAssign(item, quantity);
    }

    private bool CanAssignQuickSlot(int itemId)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);
        return item != null && item.canQuickSlot;
    }
    #endregion

    #region UI 제어
    /// <summary>
    /// 인벤토리 UI 열기
    /// </summary>
    public void OpenInventory()
    {
        InventoryUIRoot.SetActive(true);
        UpdateInventoryUI();
        Debug.Log("[인벤토리] UI 열림");
    }

    /// <summary>
    /// 인벤토리 UI 닫기
    /// </summary>
    public void CloseInventory()
    {
        InventoryUIRoot.SetActive(false);
        Debug.Log("[인벤토리] UI 닫힘");
    }
    #endregion

    #region 디버그 & 테스트
    [ContextMenu("Debug/Update UI")]
    public void CMD_UpdateUI()
    {
        UpdateInventoryUI();
    }

    [ContextMenu("Debug/Add Random Item")]
    public void CMD_AddRandomItem()
    {
        int[] testItemIds = { 4102001, 4102002, 4102005 };
        int randomId = testItemIds[Random.Range(0, testItemIds.Length)];
        AddItem(randomId, Random.Range(1, 10));
    }

    [ContextMenu("Debug/Print Inventory Status")]
    public void CMD_PrintInventoryStatus()
    {
        Debug.Log($"=== 인벤토리 상태 ===");
        Debug.Log($"사용중인 슬롯: {_items.Count}/{maxInventorySlots}");

        foreach (var item in _items)
        {
            ItemBase itemData = ItemDatabase.I.GetItem(item.Key);
            Debug.Log($"- {itemData.name} (ID: {item.Key}): {item.Value}개");
        }
    }
    #endregion

    #region 유니티 이벤트
    private void Awake()
    {
        quickSlot.OnSlotAssign += AssignQuickSlot;
        foreach (var text in itemsTextInfo)
        {
            if(text == null)
            Debug.LogError($"[Awake]{text.gameObject.name} 할당되지 않았습니다!");
        }
    }

    private void Start()
    {
        // 시작 시 UI 초기화
        UpdateInventoryUI();
        foreach(var text in itemsTextInfo)
        {
            if (text == null)
                Debug.LogError($"[Start]{text.gameObject.name} 없음");
        }
    }
    #endregion
}