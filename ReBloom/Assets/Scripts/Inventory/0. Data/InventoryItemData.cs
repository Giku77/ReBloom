using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemData", menuName = "ReBloom/Inventory/GameInventory Data")]
public class InventoryItemData : ScriptableObject, IItemContainer
{
    [Header("Settings")]
    [SerializeField] private int inventoryTier = 0;

    public int InventoryTier
    {
        get => inventoryTier;
        private set => inventoryTier = Mathf.Clamp(value, 0, 3);
    }

    public int SlotCount
    {
        get => inventoryTier switch
        {
            0 => 10,
            1 => 20,
            2 => 30,
            3 => 40,
            _ => 10
        };
        set
        {
            if (value <= 10) InventoryTier = 0;
            else if (value <= 20) InventoryTier = 1;
            else if (value <= 30) InventoryTier = 2;
            else InventoryTier = 3;
        }
    }

    // 고정 크기 배열 (null = 빈 슬롯)
    [SerializeField] private ItemSlotData[] slots;

    // ---- 이벤트 ----
    public event Action<int, int> OnItemAdded;
    public event Action<int, int> OnItemRemoved;
    public event Action OnInventoryChanged;
    public event Action<string> OnMessage;
    public event Action<ItemBase, int> OnItemToastMessage;
    public event Action<string, Color> OnWarningMessage;

    // ---- IItemContainer 구현 ----
    public event Action OnContainerChanged
    {
        add => OnInventoryChanged += value;
        remove => OnInventoryChanged -= value;
    }

    public IReadOnlyList<ItemSlotData> Items
    {
        get
        {
            // 실제 배열의 모든 슬롯 반환 (빈 슬롯 포함)
            var result = new List<ItemSlotData>();
            for (int i = 0; i < SlotCount; i++)
            {
                result.Add(slots[i] ?? new ItemSlotData { itemID = 0, count = 0 });
            }
            return result.AsReadOnly();
        }
    }

    public bool HasItems
    {
        get
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null && slots[i].itemID > 0)
                    return true;
            }
            return false;
        }
    }
    private void OnEnable()
    {
        // ScriptableObject 초기화 시 최대 크기로 배열 생성
        if (slots == null || slots.Length != 40)
        {
            slots = new ItemSlotData[40];
        }
    }
    // ---- 메서드 ----

    /// <summary>
    /// 아이템 추가 (스택 처리 포함)
    /// </summary>
    public int AddItem(int itemID, int count)
    {
        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null) return 0;

        int originalCount = count;
        int remainingCount = count;
        int maxStack = item.maxCount;

        // 1. 기존 스택에 추가
        if (maxStack > 1)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null && slots[i].itemID == itemID)
                {
                    int canAdd = Mathf.Min(maxStack - slots[i].count, remainingCount);
                    if (canAdd > 0)
                    {
                        slots[i].count += canAdd;
                        remainingCount -= canAdd;

                        if (remainingCount <= 0)
                        {
                            int addedAmount = originalCount;
                            OnItemAdded?.Invoke(itemID, addedAmount);
                            SendItemToastMessage(item, addedAmount);
                            OnInventoryChanged?.Invoke();
                            return addedAmount;
                        }
                    }
                }
            }
        }

        // 2. 빈 슬롯에 새로 추가
        while (remainingCount > 0)
        {
            int emptyIndex = FindFirstEmptySlot();
            if (emptyIndex == -1)
            {
                // 일부만 추가됨
                int addedCount = originalCount - remainingCount;

                if (addedCount > 0)
                {
                    OnItemAdded?.Invoke(itemID, addedCount);
                    SendItemToastMessage(item, addedCount);
                    SendWarningMessage($"인벤토리 공간 부족! {addedCount}/{originalCount}개만 습득", Color.yellow);
                }
                else
                {
                    SendWarningMessage("인벤토리가 가득 찼습니다!", Color.red);
                }

                InventroyEventSystem.InventoryFull();
                OnInventoryChanged?.Invoke();
                return addedCount; // 실제 추가된 수량 반환
            }

            int toAdd = Mathf.Min(remainingCount, maxStack);
            slots[emptyIndex] = new ItemSlotData
            {
                itemID = itemID,
                count = toAdd
            };
            remainingCount -= toAdd;
        }

        InventroyEventSystem.ItemAcquiredTier(item.tier);
        OnItemAdded?.Invoke(itemID, originalCount);
        SendItemToastMessage(item, originalCount);
        OnInventoryChanged?.Invoke();
        return originalCount;
    }

    /// <summary>
    /// 슬롯 스왑 (빈 슬롯 포함)
    /// </summary>
    public bool SwapSlots(int fromIndex, int toIndex)
    {
        // 실제 배열 크기 기준으로 검증
        if (fromIndex < 0 || fromIndex >= slots.Length ||
            toIndex < 0 || toIndex >= slots.Length)
        {
            Debug.LogError($"[SwapSlots] 범위 초과: from={fromIndex}, to={toIndex}, max={slots.Length}");
            return false;
        }

        if (fromIndex == toIndex) return true;

        // 스왑
        var temp = slots[fromIndex];
        slots[fromIndex] = slots[toIndex];
        slots[toIndex] = temp;

        OnInventoryChanged?.Invoke();
        return true;
    }

    ///// <summary>
    ///// 두 슬롯의 위치를 교환
    ///// </summary>
    //public bool SwapSlots(int fromIndex, int toIndex) //TODO: 빈슬롯 이동 미구현
    //{
    //    // 유효성 검사
    //    if (fromIndex < 0 || fromIndex >= activeSlots.Count)
    //    {
    //        Debug.LogError($"[InventoryData] 유효하지 않은 출발 인덱스: {fromIndex} (슬롯 개수: {activeSlots.Count})");
    //        return false;
    //    }

    //    if (toIndex < 0 || toIndex >= activeSlots.Count)
    //    {
    //        Debug.LogError($"[InventoryData] 유효하지 않은 도착 인덱스: {toIndex} (슬롯 개수: {activeSlots.Count})");
    //        return false;
    //    }

    //    if (fromIndex == toIndex)
    //    {
    //      //  Debug.LogWarning("[InventoryData] 같은 슬롯입니다.");
    //        return false;
    //    }

    //    // 스왑
    //    ItemSlotData temp = activeSlots[fromIndex];
    //    activeSlots[fromIndex] = activeSlots[toIndex];
    //    activeSlots[toIndex] = temp;

    //    // 디버그 로그
    //    var fromItem = ItemDatabase.I.GetItem(activeSlots[toIndex].itemID); // 스왑 후이므로 반대
    //    var toItem = ItemDatabase.I.GetItem(activeSlots[fromIndex].itemID);
    //    // Debug.Log($"[InventoryData] 슬롯 스왑: [{fromIndex}] {toItem?.itemName} ({activeSlots[fromIndex].count}개) <-> " + $"[{toIndex}] {fromItem?.itemName} ({activeSlots[toIndex].count}개)");

    //    // 변경 이벤트 발생 (UI 갱신)
    //    OnInventoryChanged?.Invoke();

    //    return true;
    //}

    /// <summary>
    /// 아이템 추가 (첫 빈 슬롯에)
    /// </summary>
    private bool AddItemAtFirstEmpty(int itemID, int count)
    {
        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null) return false;

        int remainingCount = count;

        // 1. 스택 가능한 기존 슬롯 찾기
        if (item.maxCount > 1)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null && slots[i].itemID == itemID)
                {
                    int canStack = Mathf.Min(item.maxCount - slots[i].count, remainingCount);
                    if (canStack > 0)
                    {
                        slots[i].count += canStack;
                        remainingCount -= canStack;

                        if (remainingCount <= 0)
                        {
                            OnItemAdded?.Invoke(itemID, count);
                            OnInventoryChanged?.Invoke();
                            return true;
                        }
                    }
                }
            }
        }

        // 2. 빈 슬롯에 추가
        while (remainingCount > 0)
        {
            int emptyIndex = FindFirstEmptySlot();
            if (emptyIndex == -1)
            {
                Debug.LogWarning("인벤토리 가득참!");
                return false;
            }

            int toAdd = Mathf.Min(remainingCount, item.maxCount);
            slots[emptyIndex] = new ItemSlotData
            {
                itemID = itemID,
                count = toAdd
            };
            remainingCount -= toAdd;
        }

        OnItemAdded?.Invoke(itemID, count);
        OnInventoryChanged?.Invoke();
        return true;
    }

  /// <summary>
  /// 첫 빈 슬롯 찾기
  /// </summary>
    private int FindFirstEmptySlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null || slots[i].itemID <= 0)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 특정 슬롯 아이템 제거
    /// </summary>
    public bool RemoveAtSlot(int slotIndex, int count = -1)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return false;

        if (slots[slotIndex] == null)
            return false;

        int itemID = slots[slotIndex].itemID;
        int currentCount = slots[slotIndex].count;

        if (count == -1 || count >= currentCount)
        {
            // 전체 제거
            slots[slotIndex] = null;
            OnItemRemoved?.Invoke(itemID, currentCount);
        }
        else
        {
            // 일부 제거
            slots[slotIndex].count -= count;
            OnItemRemoved?.Invoke(itemID, count);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public bool RemoveItem(int itemID, int count)
    {
        int totalCount = GetItemCount(itemID);
        if (totalCount < count) return false;

        int remainingToRemove = count;

        // Items 대신 직접 배열 순회
        for (int i = slots.Length - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            if (slots[i] != null && slots[i].itemID == itemID)
            {
                int removeCount = Mathf.Min(slots[i].count, remainingToRemove);
                slots[i].count -= removeCount;
                
                if (slots[i].count <= 0)
                    slots[i] = null;
                
                remainingToRemove -= removeCount;
            }
        }

        OnItemRemoved?.Invoke(itemID, count);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 아이템 개수 조회 (모든 슬롯 합계)
    /// </summary>
    public int GetItemCount(int itemID)
    {
        return Items.Where(slot => slot.itemID == itemID).Sum(slot => slot.count);
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = null;
        }
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 특정 슬롯의 아이템 가져오기
    /// </summary>
    public ItemSlotData GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    /// <summary>
    /// 특정 아이템을 다른 컨테이너로 전송 (부분 성공 지원)
    /// </summary>
    public bool TransferTo(IItemContainer target, int itemID, int count)
    {
        if (target == null || GetItemCount(itemID) < count)
            return false;

        // 추가 시도
        int addedCount = target.AddItem(itemID, count);

        if (addedCount > 0)
        {
            // 실제로 추가된 만큼만 제거
            RemoveItem(itemID, addedCount);

            // 전부 성공했는지 여부 반환
            return addedCount == count;
        }

        return false;
    }

    //public bool TransferAllTo(IItemContainer target)
    //{
    //    if (target == null || !HasItems)
    //    {
    //        Debug.Log("[InventoryItemData] Transfer 실패: target null 또는 아이템 없음");
    //        return false;
    //    }

    //    var itemsCopy = Items;
    //    int totalTransferred = 0;

    //    foreach (var slot in itemsCopy)
    //    {
    //        if (slot != null && slot.itemID > 0)
    //        {
    //            target.AddItem(slot.itemID, slot.count);
    //            totalTransferred++;
    //        }
    //    }

    //    Debug.Log($"[InventoryItemData] {totalTransferred}개 아이템 전송 완료");
    //    Clear();
    //    return true;
    //}

    /// <summary>
    /// 모든 아이템을 다른 컨테이너로 전송 (부분 성공 지원)
    /// </summary>
    public bool TransferAllTo(IItemContainer target)
    {
        if (target == null || !HasItems)
        {
            Debug.Log("[InventoryItemData] Transfer 실패: target null 또는 아이템 없음");
            return false;
        }

        var itemsCopy = Items.ToList();
        bool allTransferred = true;

        foreach (var slot in itemsCopy)
        {
            if (slot != null && slot.itemID > 0)
            {
                int addedCount = target.AddItem(slot.itemID, slot.count);

                if (addedCount > 0)
                {
                    RemoveItem(slot.itemID, addedCount);
                }

                if (addedCount < slot.count)
                {
                    allTransferred = false;
                    Debug.LogWarning($"[InventoryItemData] {slot.itemID}: {addedCount}/{slot.count}개만 전송됨");
                }
            }
        }

        Debug.Log($"[InventoryItemData] 아이템 전송 완료 (전부 성공: {allTransferred})");
        return allTransferred;
    }

    // ---- Tier 확장 ----
    public bool Expand(int targetTier)
    {
        if (targetTier < 1 || targetTier > 3)
        {
            Debug.LogError($"[InventoryData] 잘못된 Tier: {targetTier} (1~3만 가능)");
            return false;
        }

        int nextTier = inventoryTier + 1;

        if (targetTier != nextTier)
        {
            if (targetTier <= inventoryTier)
            {
                Debug.LogWarning($"[InventoryData] 이미 Tier {inventoryTier}입니다.");
            }
            else
            {
                Debug.LogWarning($"[InventoryData] Tier {targetTier}는 현재 적용 불가! (다음: Tier {nextTier})");
            }
            return false;
        }

        int oldTier = inventoryTier;
        int oldSlots = SlotCount;

        InventoryTier = targetTier;

        int newSlots = SlotCount;
        int addedSlots = newSlots - oldSlots;

        Debug.Log($"[인벤토리 확장] Tier {oldTier} → {targetTier} ({oldSlots}칸 → {newSlots}칸, +{addedSlots}칸)");

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool ExpandToNextTier()
    {
        int nextTier = inventoryTier + 1;

        if (nextTier > 3)
        {
            Debug.LogWarning("[InventoryData] 이미 최대 Tier입니다!");
            return false;
        }

        return Expand(nextTier);
    }

    // ---- 초기화 ----
    public void Initialize()
    {
        int maxSlots = 40; // 최대 Tier 슬롯 수
        slots = new ItemSlotData[maxSlots];
        // 테스트 아이템
        AddItemAtFirstEmpty(4102001, 5);
        AddItemAtFirstEmpty(4102031, 3);
        AddItemAtFirstEmpty(4102007, 5);
        AddItemAtFirstEmpty(4102008, 5);
        AddItemAtFirstEmpty(4102009, 10);

        //AddItem(4102001, 5);
        //AddItem(4102031, 3);
        //AddItem(4102007, 5);
        //AddItem(4102008, 5);
        //AddItem(4102009, 10);

        inventoryTier = 0;
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(int itemId, int amount) => GetItemCount(itemId) >= amount;

    public void Cleanup()
    {
        OnItemAdded = null;
        OnItemRemoved = null;
        OnInventoryChanged = null;
        OnMessage = null;
    }

    /// <summary>
    /// 모든 슬롯 가져오기 (UI용)
    /// </summary>
    public ItemSlotData[] GetAllSlots()
    {
        // SlotCount만큼만 반환 (현재 티어의 슬롯만)
        var result = new ItemSlotData[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            result[i] = slots[i];
        }
        return result;
    }
    /// <summary>
    /// 특정 슬롯에 아이템 직접 설정 (DragDrop용)
    /// </summary>
    public bool AddItemAtSlot(int slotIndex, int itemID, int count)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return false;

        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null)
            return false;

        // 해당 슬롯에 직접 설정
        slots[slotIndex] = new ItemSlotData
        {
            itemID = itemID,
            count = Mathf.Min(count, item.maxCount)
        };

        OnItemAdded?.Invoke(itemID, count);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 슬롯 병합 (같은 아이템 스택)
    /// </summary>
    public bool TryStackItems(int sourceSlot, int targetSlot)
    {
        if (sourceSlot < 0 || sourceSlot >= SlotCount ||
            targetSlot < 0 || targetSlot >= SlotCount)
            return false;

        var source = slots[sourceSlot];
        var target = slots[targetSlot];

        if (source == null || target == null)
            return false;

        // 같은 아이템이고 스택 가능한 경우
        if (source.itemID == target.itemID)
        {
            var item = ItemDatabase.I.GetItem(source.itemID);
            if (item != null && item.maxCount > 1)
            {
                int canStack = Mathf.Min(item.maxCount - target.count, source.count);
                if (canStack > 0)
                {
                    target.count += canStack;
                    source.count -= canStack;

                    // 소스 슬롯이 비면 제거
                    if (source.count <= 0)
                    {
                        slots[sourceSlot] = null;
                    }

                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 슬롯 분할 (아이템 일부만 이동)
    /// </summary>
    public bool SplitStack(int sourceSlot, int targetSlot, int amount)
    {
        if (sourceSlot < 0 || sourceSlot >= SlotCount ||
            targetSlot < 0 || targetSlot >= SlotCount)
            return false;

        var source = slots[sourceSlot];
        if (source == null || source.count <= amount)
            return false;

        // 타겟 슬롯이 비어있어야 함
        if (slots[targetSlot] != null && slots[targetSlot].itemID > 0)
            return false;

        // 분할
        slots[targetSlot] = new ItemSlotData
        {
            itemID = source.itemID,
            count = amount
        };
        source.count -= amount;

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 빈 슬롯인지 확인
    /// </summary>
    public bool IsEmptySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return false;

        return slots[slotIndex] == null || slots[slotIndex].itemID <= 0;
    }

    /// <summary>
    /// 특정 슬롯 비우기
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return;

        if (slots[slotIndex] != null)
        {
            int itemID = slots[slotIndex].itemID;
            int count = slots[slotIndex].count;

            slots[slotIndex] = null;

            OnItemRemoved?.Invoke(itemID, count);
            OnInventoryChanged?.Invoke();
        }
    }
    private void SendItemToastMessage(ItemBase item, int amount)
        => OnItemToastMessage?.Invoke(item, amount);

    private void SendWarningMessage(string message, Color color)
        => OnWarningMessage?.Invoke(message, color);
}