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

    [Header("Overlays")]
    [SerializeField] private GameObject lockOverlay;      // 잠금 패널
    [SerializeField] private GameObject completedOverlay; // (옵션)
    [SerializeField] private GameObject lackOverlay;      // (옵션)

    private GreenhouseUpgradeRowData _row;
    private Action<int> _onClick;

    private void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(() =>
            {
                if (_row != null) _onClick?.Invoke(_row.upgradeId);
            });
    }

    public void Bind(GreenhouseUpgradeRowData row, Action<int> onClick)
    {
        _row = row;
        _onClick = onClick;

        if (titleText != null)
            titleText.text = row.upgradeName;

        if (costText != null)
            costText.text = BuildCostText(row);
    }

    public void RefreshState(
        GreenhouseUpgradeState state,
        GreenhouseUpgradeRowData row,
        bool completed,
        bool unlocked,
        bool affordable)
    {
        // 잠금: 아직 해금 안 됨
        if (lockOverlay != null)
            lockOverlay.SetActive(!completed && !unlocked);

        // 완료
        if (completedOverlay != null)
            completedOverlay.SetActive(completed);

        // 재료 부족
        if (lackOverlay != null)
            lackOverlay.SetActive(!completed && unlocked && !affordable);

        // 버튼 활성
        if (actionButton != null)
            actionButton.interactable = (!completed && unlocked && affordable);
    }

    private string BuildCostText(GreenhouseUpgradeRowData row)
    {
        var parts = new System.Collections.Generic.List<string>();
        var itemName1 = ItemDatabase.I.GetItem(row.needItem1)?.itemName ?? "???";
        var itemName2 = ItemDatabase.I.GetItem(row.needItem2)?.itemName ?? "???";

        if (row.needItem1 != 0 && row.needCount1 > 0)
            parts.Add($"{itemName1} x{row.needCount1}");
        if (row.needItem2 != 0 && row.needCount2 > 0)
            parts.Add($"{itemName2} x{row.needCount2}");

        return parts.Count > 0 ? string.Join("  ", parts) : "";
    }
}
