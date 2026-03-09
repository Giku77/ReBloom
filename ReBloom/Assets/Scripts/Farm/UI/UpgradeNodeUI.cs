using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeNodeUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button actionButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Overlays")]
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private GameObject completedOverlay;
    [SerializeField] private GameObject lackOverlay;

    private GreenhouseUpgradeRowData row;
    private Action<int> onClick;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(() =>
            {
                if (row != null)
                    onClick?.Invoke(row.upgradeId);
            });
        }
    }

    public void Bind(GreenhouseUpgradeRowData upgradeRow, Action<int> clickHandler)
    {
        row = upgradeRow;
        onClick = clickHandler;

        if (titleText != null)
            titleText.text = upgradeRow.upgradeName;

        if (costText != null)
            costText.text = BuildCostText(upgradeRow);
    }

    public void RefreshState(
        GreenhouseUpgradeState state,
        GreenhouseUpgradeRowData upgradeRow,
        bool completed,
        bool unlocked,
        bool affordable)
    {
        if (lockOverlay != null)
            lockOverlay.SetActive(!completed && !unlocked);

        if (completedOverlay != null)
            completedOverlay.SetActive(completed);

        if (lackOverlay != null)
            lackOverlay.SetActive(!completed && unlocked && !affordable);

        if (actionButton != null)
            actionButton.interactable = !completed && unlocked;

        if (canvasGroup != null)
        {
            if (completed)
                canvasGroup.alpha = 1f;
            else if (!unlocked)
                canvasGroup.alpha = 0.45f;
            else if (!affordable)
                canvasGroup.alpha = 0.85f;
            else
                canvasGroup.alpha = 1f;
        }
    }

    private string BuildCostText(GreenhouseUpgradeRowData upgradeRow)
    {
        var parts = new System.Collections.Generic.List<string>();
        var itemName1 = ItemDatabase.I.GetItem(upgradeRow.needItem1)?.itemName ?? "???";
        var itemName2 = ItemDatabase.I.GetItem(upgradeRow.needItem2)?.itemName ?? "???";

        if (upgradeRow.needItem1 != 0 && upgradeRow.needCount1 > 0)
            parts.Add($"{itemName1} x{upgradeRow.needCount1}");
        if (upgradeRow.needItem2 != 0 && upgradeRow.needCount2 > 0)
            parts.Add($"{itemName2} x{upgradeRow.needCount2}");

        return parts.Count > 0 ? string.Join("  ", parts) : string.Empty;
    }
}