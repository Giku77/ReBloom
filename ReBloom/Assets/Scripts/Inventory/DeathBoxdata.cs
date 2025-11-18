using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 사망 시 드롭되는 시체 박스 데이터
/// 위치 정보와 아이템 목록을 저장
/// </summary>
[CreateAssetMenu(fileName = "New DeathBox Data", menuName = "ReBloom/Inventory/DeathBox Data")]
public class DeathBoxData : ScriptableObject
{
    [Header("Death Info")]
    [SerializeField] private string deathBoxID; // 고유 ID (복수 시체박스 대비)
    [SerializeField] private Vector3 deathPosition; // 사망 위치
    [SerializeField] private DateTime deathTime; // 사망 시간

    [Header("Item Data")]
    [SerializeField] private List<ItemSlotData> storedItems = new List<ItemSlotData>();

    // 이벤트: 시체박스 데이터 변경 시
    public event Action OnDeathBoxDataChanged;

    #region Properties
    public string DeathBoxID => deathBoxID;
    public Vector3 DeathPosition => deathPosition;
    public DateTime DeathTime => deathTime;
    public IReadOnlyList<ItemSlotData> StoredItems => storedItems;
    public bool HasItems => storedItems.Count > 0;
    #endregion

    /// <summary>
    /// 인벤토리 데이터에서 시체박스로 아이템 이동
    /// </summary>
    public void StoreItemsFromInventory(InventoryItemData inventoryData, Vector3 position)
    {
        if (inventoryData == null)
        {
            Debug.LogError("[DeathBoxData] InventoryItemData가 null입니다!");
            return;
        }

        // 기존 데이터 초기화
        Clear();

        // 사망 정보 설정
        deathBoxID = Guid.NewGuid().ToString(); // 고유 ID 생성
        deathPosition = position;
        deathTime = DateTime.Now;

        // 인벤토리 아이템을 시체박스로 복사
        foreach (var slot in inventoryData.GetAllItems())
        {
            if (slot.Value > 0)
            {
                storedItems.Add(new ItemSlotData
                {
                    itemID = slot.Key,
                    count = slot.Value
                });
            }
        }

        OnDeathBoxDataChanged?.Invoke();
    }

    /// <summary>
    /// 시체박스에서 인벤토리로 아이템 회수
    /// </summary>
    public void RetrieveItemsToInventory(InventoryItemData inventoryData)
    {
        if (inventoryData == null)
        {
            Debug.LogError("[DeathBoxData] InventoryItemData가 null입니다!");
            return;
        }

        if (!HasItems)
        {
            Debug.LogWarning("[DeathBoxData] 시체박스가 비어있습니다!");
            return;
        }

        // 시체박스 아이템을 인벤토리로 이동
        foreach (var slot in storedItems)
        {
           inventoryData.AddItem(slot.itemID, slot.count);
        }

        Debug.Log($"[DeathBoxData] {storedItems.Count}개 아이템을 인벤토리로 회수했습니다.");

        // 시체박스 비우기
        Clear();
    }

    /// <summary>
    /// 특정 아이템만 회수
    /// </summary>
    public bool TryRetrieveItem(int itemID, int count, InventoryItemData inventoryData)
    {
        var slot = storedItems.Find(s => s.itemID == itemID);
        if (slot == null || slot.count < count)
        {
            return false;
        }

        // 인벤토리에 추가
        inventoryData.AddItem(itemID, count);

        // 시체박스에서 제거
        slot.count -= count;
        if (slot.count <= 0)
        {
            storedItems.Remove(slot);
        }

        OnDeathBoxDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 시체박스 비우기
    /// </summary>
    public void Clear()
    {
        storedItems.Clear();
        deathBoxID = string.Empty;
        deathPosition = Vector3.zero;
        OnDeathBoxDataChanged?.Invoke();
    }

    /// <summary>
    /// 특정 아이템 개수 조회
    /// </summary>
    public int GetItemCount(int itemID)
    {
        var slot = storedItems.Find(s => s.itemID == itemID);
        return slot?.count ?? 0;
    }
}

/// <summary>
/// 시체박스 슬롯 데이터
/// </summary>
[Serializable]
public class ItemSlotData
{
    public int itemID;
    public int count;
}