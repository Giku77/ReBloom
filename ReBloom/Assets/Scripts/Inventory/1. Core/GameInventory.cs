using UnityEngine;

public class GameInventory : MonoBehaviour, IInventoryProvider
{
    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [Header("UI References")]
    [SerializeField] private GameInventoryUI inventoryUI;
    [SerializeField] private QuickSlot quickSlot;

    #region IInventoryProvider 구현
    public int GetItemCount(int itemId)
    {
        return inventoryData.GetItemCount(itemId);
    }

    public void AddItem(int itemId, int amount)
    {
        inventoryData.AddItem(itemId, amount);
    }

    public void RemoveItem(int itemId, int amount)
    {
        inventoryData.RemoveItem(itemId, amount);
    }

    public void Clear()
    {
        inventoryData.Clear();
    }

    public bool HasItem(int itemId, int amount)
    {
        return inventoryData.HasItem(itemId, amount);
    }

    public void Consume(int itemId, int amount)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);

        if (item != null && item.canUseable)
        {
            RemoveItem(itemId, amount);
            inventoryData.SendMessage($"{item.itemName}을(를) {amount}개 사용했습니다.");
        }
        else
        {
            inventoryData.SendMessage($"{item?.itemName ?? "알 수 없는 아이템"}은(는) 사용할 수 없습니다.");
        }
    }
    #endregion

    #region 퀵슬롯 관련
    private void Awake()
    {
        if (quickSlot != null)
        {
            quickSlot.OnSlotAssign += AssignQuickSlot;
        }
    }
    private void OnDestroy()
    {
        if (quickSlot != null)
        {
            quickSlot.OnSlotAssign -= AssignQuickSlot;
        }
    }

    public void TryAssignQuickSlot(int itemId)
    {
        if (!inventoryData.Items.ContainsKey(itemId))
        {
            inventoryData.SendMessage("인벤토리에 해당 아이템이 없습니다.");
            return;
        }

        if (CanAssignQuickSlot(itemId))
        {
            ItemBase item = ItemDatabase.I.GetItem(itemId);
            AssignQuickSlot(item, GetItemCount(itemId));
            inventoryData.SendMessage($"{item.itemName}을(를) 퀵슬롯에 배치했습니다.");
        }
        else
        {
            ItemBase item = ItemDatabase.I.GetItem(itemId);
            inventoryData.SendMessage($"{item.itemName}은(는) 퀵슬롯에 배치할 수 없습니다.");
        }
    }

    /// <summary>
    /// 퀵슬롯 자동 배치 (수정됨)
    /// </summary>
    public int AutoFillQuickSlots()
    {
        if (quickSlot == null)
        {
            Debug.LogWarning("[GameInventory] QuickSlot이 할당되지 않았습니다!");
            return 0;
        }

        int filledCount = 0;

        foreach (var itemPair in inventoryData.Items)
        {
            int itemId = itemPair.Key;
            int quantity = itemPair.Value;

            // 퀵슬롯 배치 가능 여부 확인
            if (CanAssignQuickSlot(itemId))
            {
                ItemBase item = ItemDatabase.I.GetItem(itemId);

                if (quickSlot.IsItemAlreadyAssigned(item))
                {
                    Debug.Log($"[퀵슬롯] {item.itemName}은(는) 이미 배치되어 있음 (스킵)");
                    continue;
                }

                // 배치 시도
                if (quickSlot.TryAssign(item, quantity))
                {
                    filledCount++;
                    Debug.Log($"[퀵슬롯] {item.itemName} x{quantity} 배치 성공");
                }
                else
                {
                    // 슬롯이 가득 찬 경우 중단
                    inventoryData.SendMessage($"퀵슬롯이 가득 참 (총 {filledCount}개 배치)");
                    Debug.Log($"[퀵슬롯] 슬롯이 가득 참");
                    break;
                }
            }
            else
            {
                Debug.Log($"[퀵슬롯] {ItemDatabase.I.GetItem(itemId)?.itemName}은(는) 퀵슬롯 불가");
            }
        }

        // 결과 메시지
        if (filledCount > 0)
        {
            inventoryData.SendMessage($"퀵슬롯에 {filledCount}개 아이템 배치 완료!");
        }
        else
        {
            inventoryData.SendMessage("퀵슬롯에 배치할 수 있는 아이템이 없습니다.");
        }

        return filledCount;
    }

    public void AssignQuickSlot(ItemBase item, int quantity)
    {
        if (quickSlot != null)
        {
            quickSlot.TryAssign(item, quantity);
        }
    }

    private bool CanAssignQuickSlot(int itemId)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);
        return item != null && item.canQuickSlot;
    }
    #endregion

    #region UI 제어
    public void OpenInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("[GameInventory] InventoryUI가 할당되지 않았습니다!");
        }
    }

    public void CloseInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.ToggleInventory();
        }
    }
    #endregion

    #region 디버그
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
        Debug.Log("=== 인벤토리 상태 ===");
        Debug.Log($"사용중인 슬롯: {inventoryData.Items.Count}/{inventoryData.MaxSlots}");

        foreach (var item in inventoryData.Items)
        {
            ItemBase itemData = ItemDatabase.I.GetItem(item.Key);
            Debug.Log($"- {itemData.itemName} (ID: {item.Key}): {item.Value}개");
        }
    }

    [ContextMenu("Debug/Auto Fill QuickSlots")]
    public void CMD_AutoFillQuickSlots()
    {
        AutoFillQuickSlots();
    }
    #endregion
}