using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;

public class MainSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private GameObject loadingOverlay; // 검은 패널 + 로딩텍스트
    [SerializeField] private InitialItemSpawner initialItemSpawner;
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("Mobile Warmup")]
    [SerializeField] private int warmCountPerIdMobile = 1;   // 모바일은 1~2 추천
    [SerializeField] private int msBudgetPerFrame = 4;       // 프레임당 워밍업 예산(3~6ms 추천)

    private async void Start()
    {
        if (loadingOverlay) loadingOverlay.SetActive(true);

        if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
        if (initialItemSpawner == null) initialItemSpawner = FindFirstObjectByType<InitialItemSpawner>();

        await UniTask.WaitUntil(() => ItemDatabase.I.IsInitialized);

        // 초반 스폰 예정 ID만
        var ids = initialItemSpawner != null
            ? initialItemSpawner.GetPlannedItemIDs()
            : Array.Empty<int>();

        // 모바일이면 최소만 워밍업
        int warmCount = Application.isMobilePlatform ? warmCountPerIdMobile : 2;

        await PrewarmItemPools(ids, warmCount, msBudgetPerFrame, this.GetCancellationTokenOnDestroy());
        await initialItemSpawner.Begin();

        if (loadingOverlay) loadingOverlay.SetActive(false);

        // 여기서 컷씬 시작 트리거(너 프로젝트 방식에 맞춰 호출)
        // CutSceneManager.I.PlayIntro(); 같은거
    }

    private async UniTask PrewarmItemPools(int[] ids, int warmCountPerId, int frameBudgetMs, CancellationToken ct)
    {
        if (itemSpawner == null || ids == null || ids.Length == 0) return;

        var sw = new Stopwatch();

        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();

            // 풀 생성 + 워밍업 (Get/Release로 1개만 만들어 두기
            await itemSpawner.PrewarmOneItemAsync(id, warmCountPerId, ct);

            // 프레임 예산 분산
            sw.Restart();
            while (sw.ElapsedMilliseconds < frameBudgetMs)
                await UniTask.Yield();
        }
    }
}
