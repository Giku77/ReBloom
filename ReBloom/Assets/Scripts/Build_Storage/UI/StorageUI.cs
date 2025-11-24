using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 창고 UI 관리자 - 슬롯 생성 및 데이터 동기화
/// </summary>
public class StorageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldStorage worldStorage;
    [SerializeField] private InventoryItemData playerInventory;
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Settings")]
    [SerializeField] private int maxSlots = 20;

    private List<StorageSlot> slots = new List<StorageSlot>();
    private StorageData storageData;

    private void Awake()
    {
        CreateSlots();
    }

    private void OnEnable()
    {
        if (worldStorage != null)
        {
            // WorldStorage에서 StorageData 가져오기
            storageData = worldStorage.GetComponent<StorageData>();

            if (storageData != null)
            {
                storageData.OnStorageChanged += RefreshUI;
                RefreshUI();
            }
        }
    }

    private void OnDisable()
    {
        if (storageData != null)
        {
            storageData.OnStorageChanged -= RefreshUI;
        }
    }

    private void CreateSlots()
    {
        slots.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            var slotObj = Instantiate(slotPrefab, slotParent);
            var slot = slotObj.GetComponent<StorageSlot>();

            if (slot != null)
            {
                slots.Add(slot);
                slot.Clear();
            }
        }
    }

    private void RefreshUI()
    {
        if (storageData == null) return;

        // 모든 슬롯 초기화
        foreach (var slot in slots)
        {
            slot.Clear();
        }
        // 창고 데이터 표시
        var items = storageData.GetAllItems();
        for (int i = 0; i < items.Count && i < slots.Count; i++)
        {
            var itemStack = items[i];
            if (itemStack.itemID != 0)
            {
                var itemBase = ItemDatabase.I.GetItem(itemStack.itemID);
                if (itemBase != null)
                {
                    slots[i].SetItem(itemBase, itemStack.count);
                }
            }
        }

        Debug.Log($"[StorageUI] UI 갱신 완료: {items.Count}개 아이템");
    }

    /// <summary>
    /// 더블클릭으로 아이템 회수
    /// </summary>
    public void WithdrawItem(int slotIndex)
    {
        if (storageData == null || playerInventory == null)
        {
            Debug.LogError("[StorageUI] 데이터가 없습니다!");
            return;
        }

        var items = storageData.GetAllItems();
        if (slotIndex < 0 || slotIndex >= items.Count)
            return;

        var itemStack = items[slotIndex];
        if (itemStack.itemID == 0)
            return;

            // 인벤토리에 추가
            bool added = playerInventory.AddItem(itemStack.itemID, itemStack.count);

            if (added)
            {
                // 창고에서 제거
                storageData.RemoveItem(itemStack.itemID, itemStack.count);
                Debug.Log($"[StorageUI] {itemStack.count}개 회수 완료");
            }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}