using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// 풀링된 아이템이 자기 자신을 풀로 반환할 수 있도록 하는 헬퍼 컴포넌트
/// ItemSpawner의 오브젝트 풀에서 생성된 GameObject에 자동으로 추가됨
/// </summary>
public class PooledItem : MonoBehaviour
{
    // 이 아이템을 생성한 Spawner 참조
    private ItemSpawner spawner;

    // 이 아이템의 ID (어떤 풀로 돌아갈지 식별)
    private int itemID;

    public int ItemID => itemID;

    // 중복 반환 방지 플래그
    private int returnToken;

    private CancellationTokenSource disableCts;

    /// <summary>
    /// 초기화 - ItemSpawner에서 호출
    /// </summary>
    public void Initialize(ItemSpawner itemSpawner, int id)
    {
        spawner = itemSpawner;
        itemID = id;
    }

    /// <summary>
    /// 이 GameObject를 풀로 반환
    /// </summary>
    public void ReturnToPool()
    {
        // 즉시 반환
        if (spawner != null)
            spawner.ReturnToPool(gameObject, itemID);
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 제한 시간 뒤 아이템 삭제를 위한 메서드
    /// </summary>
    // UniTask 기반 비동기 지연 반환
    public void ReturnToPoolAfterDelay(float delaySeconds)
    {
        if (disableCts == null)
            disableCts = new CancellationTokenSource();

        var ct = disableCts.Token;
        DelayedReturnAsync(delaySeconds, ct).Forget();
    }

    private async UniTask DelayedReturnAsync(float delaySeconds, CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);
            if (ct.IsCancellationRequested) return;
            ReturnToPool();
        }
        catch (OperationCanceledException)
        {
            // 취소된 경우 안전하게 무시
        }
    }

    private void OnEnable()
    {
        // 재활용 시 새 토큰 생성
        disableCts?.Cancel();
        disableCts?.Dispose();
        disableCts = new CancellationTokenSource();
    }

    private void OnDisable()
    {
        // 비활성화되면 예약된 딜레이를 취소
        disableCts?.Cancel();
    }

    private void OnDestroy()
    {
        // 정리
        disableCts?.Dispose();
        disableCts = null;
    }
}