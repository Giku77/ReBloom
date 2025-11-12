using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ItemSpawner;

/// <summary>
/// 오브젝트 풀 상태를 실시간으로 표시하는 디버그 UI
/// </summary>
public class PoolDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private GameObject uiRoot;

    [Header("UI Elements")]
    [SerializeField] private Transform poolListContainer;
    [SerializeField] private GameObject poolInfoPrefab;
    [SerializeField] private TextMeshProUGUI txtSummary;
    [SerializeField] private Button btnRefresh;
    [SerializeField] private Button btnClearAll;

    [Header("Settings")]
    [SerializeField] private float autoRefreshInterval = 1f;

    private List<PoolInfoDisplay> displays = new List<PoolInfoDisplay>();
    private float lastRefreshTime;

    private void Awake()
    {
        if (itemSpawner == null)
        {
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
        }

        btnRefresh?.onClick.AddListener(RefreshDisplay);
        btnClearAll?.onClick.AddListener(ClearAllPools);

        uiRoot.SetActive(false);
    }

    private void Update()
    {
        if (!uiRoot.activeSelf) return;

        // 자동 새로고침
        if (Time.time - lastRefreshTime >= autoRefreshInterval)
        {
            RefreshDisplay();
        }
    }

    public void ToggleUI()
    {
        bool newState = !uiRoot.activeSelf;
        uiRoot.SetActive(newState);

        if (newState)
        {
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        if (itemSpawner == null) return;

        lastRefreshTime = Time.time;

        // 기존 표시 제거
        ClearDisplays();

        // 모든 풀 정보 가져오기
        var poolInfos = itemSpawner.GetAllPoolInfo();
        var statistics = itemSpawner.Statistics;

        // 풀별 정보 표시
        foreach (var info in poolInfos)
        {
            CreatePoolDisplay(info, statistics.GetStats(info.ItemID));
        }

        // 요약 정보 업데이트
        UpdateSummary(poolInfos, statistics);
    }

    private void CreatePoolDisplay(PoolInfo poolInfo, PoolItemStats stats)
    {
        if (poolInfoPrefab == null || poolListContainer == null) return;

        GameObject displayObj = Instantiate(poolInfoPrefab, poolListContainer);
        PoolInfoDisplay display = displayObj.GetComponent<PoolInfoDisplay>();

        if (display != null)
        {
            display.Initialize(poolInfo, stats);
            displays.Add(display);
        }
    }

    private void UpdateSummary(List<PoolInfo> poolInfos, PoolStatistics statistics)
    {
        if (txtSummary == null) return;

        int totalPools = poolInfos.Count;
        int totalActive = 0;
        int totalInactive = 0;
        int totalCreated = 0;
        int totalGets = 0;
        int totalReleases = 0;

        foreach (var info in poolInfos)
        {
            totalActive += info.CountActive;
            totalInactive += info.CountInactive;

            var stats = statistics.GetStats(info.ItemID);
            if (stats != null)
            {
                totalCreated += stats.TotalCreated;
                totalGets += stats.TotalGets;
                totalReleases += stats.TotalReleases;
            }
        }

        txtSummary.text = $"<b>오브젝트 풀 요약</b>\n" +
                          $"풀 개수: {totalPools}\n" +
                          $"활성 오브젝트: {totalActive}\n" +
                          $"비활성 오브젝트: {totalInactive}\n" +
                          $"총 생성: {totalCreated}\n" +
                          $"총 Get: {totalGets}\n" +
                          $"총 Release: {totalReleases}\n" +
                          $"재사용률: {(totalGets > 0 ? (float)totalReleases / totalGets * 100f : 0f):F1}%";
    }

    private void ClearDisplays()
    {
        foreach (var display in displays)
        {
            if (display != null)
            {
                Destroy(display.gameObject);
            }
        }
        displays.Clear();
    }

    private void ClearAllPools()
    {
        if (itemSpawner != null)
        {
            itemSpawner.ClearAllPools();
            RefreshDisplay();
        }
    }
}