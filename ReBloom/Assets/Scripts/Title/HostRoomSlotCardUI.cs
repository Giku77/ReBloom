using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostRoomSlotCardUI : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Graphic selectionGraphic;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.80f, 0.91f, 1.00f, 1.00f);

    private WorldSlotMetaDTO slotMeta;
    private Action<WorldSlotMetaDTO> onSelected;
    private Action<WorldSlotMetaDTO> onDelete;

    public string SlotId => slotMeta?.slotId;

    private void Awake()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();
    }

    public void Bind(WorldSlotMetaDTO slot, Action<WorldSlotMetaDTO> onSelectedCallback, Action<WorldSlotMetaDTO> onDeleteCallback = null)
    {
        slotMeta = slot;
        onSelected = onSelectedCallback;
        onDelete = onDeleteCallback;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(slot.displayName) ? slot.slotId : slot.displayName;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelectClick);
            selectButton.onClick.AddListener(HandleSelectClick);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(HandleDeleteClick);
            deleteButton.onClick.AddListener(HandleDeleteClick);
            deleteButton.gameObject.SetActive(onDeleteCallback != null);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionGraphic != null)
            selectionGraphic.color = selected ? selectedColor : normalColor;
    }

    private void HandleSelectClick()
    {
        onSelected?.Invoke(slotMeta);
    }

    private void HandleDeleteClick()
    {
        onDelete?.Invoke(slotMeta);
    }
}