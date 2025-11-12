using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ItemSpawner;

/// <summary>
/// 개별 풀의 정보를 표시하는 UI 요소
/// </summary>
public class PoolInfoDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI txtItemName;
    [SerializeField] private TextMeshProUGUI txtItemID;
    [SerializeField] private TextMeshProUGUI txtActive;
    [SerializeField] private TextMeshProUGUI txtInactive;
    [SerializeField] private TextMeshProUGUI txtTotal;
    [SerializeField] private TextMeshProUGUI txtStats;
    [SerializeField] private Image imgCacheStatus;

    public void Initialize(PoolInfo poolInfo, PoolItemStats stats)
    {
        if (txtItemName != null)
        {
            txtItemName.text = poolInfo.ItemName;
        }

        if (txtItemID != null)
        {
            txtItemID.text = $"ID: {poolInfo.ItemID}";
        }

        if (txtActive != null)
        {
            txtActive.text = $"활성: {poolInfo.CountActive}";
            txtActive.color = poolInfo.CountActive > 0 ? Color.green : Color.gray;
        }

        if (txtInactive != null)
        {
            txtInactive.text = $"대기: {poolInfo.CountInactive}";
        }

        if (txtTotal != null)
        {
            txtTotal.text = $"전체: {poolInfo.CountAll}";
        }

        if (stats != null && txtStats != null)
        {
            txtStats.text = $"생성: {stats.TotalCreated} | Get: {stats.TotalGets} | Release: {stats.TotalReleases}";
        }

        if (imgCacheStatus != null)
        {
            imgCacheStatus.color = poolInfo.IsCached ? Color.green : Color.red;
        }
    }
}