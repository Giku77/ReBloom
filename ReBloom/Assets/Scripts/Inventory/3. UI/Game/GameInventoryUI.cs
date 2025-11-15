using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInventoryUI : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [Header("UI References")]
    [SerializeField] private GameObject inventoryUIRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private List<TextMeshProUGUI> itemSlotTexts = new List<TextMeshProUGUI>();

    private void Awake()
    {
        if (itemSlotTexts == null || itemSlotTexts.Count == 0 || itemSlotTexts[0] == null)
        {
            AutoFindItemSlots();
        }
    }

    private void Start()
    {
        if (inventoryData == null)
        {
            Debug.LogError("[InventoryUI] InventoryData가 할당되지 않았습니다!", this);
            return;
        }

        inventoryData.OnInventoryChanged += RefreshUI;
        inventoryData.OnMessage += ShowMessage;

        inventoryData.Initialize();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (inventoryData != null)
        {
            inventoryData.OnInventoryChanged -= RefreshUI;
            inventoryData.OnMessage -= ShowMessage;
        }
    }

    public void RefreshUI()
    {
        if (itemSlotTexts == null || itemSlotTexts.Count == 0)
        {
            Debug.LogWarning("[InventoryUI] itemSlotTexts가 비어있습니다!", this);
            return;
        }

        int index = 0;

        foreach (var itemPair in inventoryData.Items)
        {
            if (index >= itemSlotTexts.Count) break;

            int itemId = itemPair.Key;
            int quantity = itemPair.Value;
            ItemBase item = ItemDatabase.I.GetItem(itemId);

            if (item != null)
            {
                itemSlotTexts[index].text = $"{item.itemName} / 수량: {quantity}개";
                itemSlotTexts[index].color = Color.white;
            }
            else
            {
                itemSlotTexts[index].text = $"알 수 없는 아이템({itemId})";
                itemSlotTexts[index].color = Color.red;
            }

            index++;
        }

        for (int i = index; i < itemSlotTexts.Count; i++)
        {
            itemSlotTexts[i].text = "빈 슬롯";
            itemSlotTexts[i].color = Color.gray;
        }
    }

    private void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            Debug.Log($"[인벤토리 메시지] {msg}");
        }
    }

    public void ToggleInventory()
    {
        if (inventoryUIRoot != null)
        {
            bool isActive = !inventoryUIRoot.activeSelf;
            inventoryUIRoot.SetActive(isActive);

            if (isActive) RefreshUI();
        }
    }

    private void AutoFindItemSlots()
    {
        if (inventoryUIRoot == null) return;

        itemSlotTexts.Clear();
        var allTmps = inventoryUIRoot.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var tmp in allTmps)
        {
            if (tmp.name.StartsWith("ItemInfo"))
            {
                itemSlotTexts.Add(tmp);
            }
        }

        itemSlotTexts.Sort((a, b) => string.Compare(a.name, b.name));
        Debug.Log($"[InventoryUI] {itemSlotTexts.Count}개의 슬롯 찾음");
    }
}