using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScanController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform scanOrigin;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private InputAction scanAction;
    [SerializeField] private RobotAnimationController animController;

    [Header("Scan Visual")]
    [SerializeField] private ScanRingEffect ringPrefab;

    [Header("Scan Settings")]
    [SerializeField] private float scanRadius = 15f;
    [SerializeField] private float ringSpeed = 10f;
    [SerializeField] private float highlightDuration = 2f;
    [SerializeField] private float cooldown = 2.5f;  // 2.5초로 변경

    private float lastScanTime = -999f;
    private CancellationToken destroyToken;

    // ===== 외부에서 쿨타임 상태를 읽기 위한 프로퍼티 =====

    /// <summary>
    /// 현재 쿨타임 중인지 여부
    /// </summary>
    public bool IsOnCooldown => Time.time < lastScanTime + cooldown;

    /// <summary>
    /// 쿨타임 진행률 (0 = 쿨타임 완료/사용가능, 1 = 방금 사용함)
    /// UI 슬라이더용: 1에서 시작해서 0으로 줄어듦
    /// </summary>
    public float CooldownProgress
    {
        get
        {
            if (!IsOnCooldown) return 0f;
            float elapsed = Time.time - lastScanTime;
            return 1f - Mathf.Clamp01(elapsed / cooldown);
        }
    }

    /// <summary>
    /// 남은 쿨타임 시간 (초)
    /// </summary>
    public float RemainingCooldown
    {
        get
        {
            if (!IsOnCooldown) return 0f;
            return Mathf.Max(0f, (lastScanTime + cooldown) - Time.time);
        }
    }

    /// <summary>
    /// 쿨타임 충전률 (0 = 방금 사용함, 1 = 충전 완료/사용 가능)
    /// UI 슬라이더용: 0에서 시작해서 1로 차오름
    /// </summary>
    public float CooldownFillAmount
    {
        get
        {
            if (!IsOnCooldown) return 1f;  // 사용 가능 = 꽉 참
            float elapsed = Time.time - lastScanTime;
            return Mathf.Clamp01(elapsed / cooldown);
        }
    }

    // ===== 기존 코드 =====

    private void Awake()
    {
        destroyToken = this.GetCancellationTokenOnDestroy();
    }

    private void OnEnable()
    {
        scanAction.Enable();
        scanAction.performed += OnScanPerformed;
    }

    private void OnDisable()
    {
        scanAction.performed -= OnScanPerformed;
        scanAction.Disable();
    }

    private void OnScanPerformed(InputAction.CallbackContext context)
    {
        TriggerScan();
    }

    /// <summary>
    /// 스캔 실행. 모바일 버튼에서도 이 메서드를 호출하면 됨
    /// </summary>
    /// <returns>스캔이 실행되었으면 true, 쿨타임 중이면 false</returns>
    public bool TriggerScan()
    {
        Debug.Log($"[ScanController] 내 InstanceID: {GetInstanceID()}");
        Debug.Log($"[ScanController] TriggerScan 호출됨. IsOnCooldown: {IsOnCooldown}");

        Debug.Log($"[ScanController] TriggerScan 호출됨. IsOnCooldown: {IsOnCooldown}");

        if (IsOnCooldown)
        {
            Debug.Log("[ScanController] 쿨타임 중이라 스킵");
            return false;
        }

        lastScanTime = Time.time;
        Debug.Log($"[ScanController] 스캔 실행! lastScanTime: {lastScanTime}");

        if (IsOnCooldown)
            return false;

        lastScanTime = Time.time;

        if (scanOrigin == null)
        {
            Debug.LogWarning("[ScanController] scanOrigin not set");
            return false;
        }

        animController?.PlayAnimation("Scan");

        Vector3 origin = scanOrigin.position;

        if (ringPrefab != null)
        {
            var ring = Instantiate(ringPrefab);
            ring.Play(origin, destroyToken);
        }

        Collider[] hits = Physics.OverlapSphere(origin, scanRadius, interactableMask);
        foreach (var hit in hits)
        {
            var outline = hit.GetComponentInParent<OutlineToggle>();
            if (outline == null) continue;

            float distance = Vector3.Distance(origin, hit.transform.position);
            float delay = distance / Mathf.Max(ringSpeed, 0.01f);
            HighlightWithDelayAsync(outline, delay, destroyToken).Forget();
        }

        return true;
    }

    private async UniTaskVoid HighlightWithDelayAsync(
        OutlineToggle outline,
        float delay,
        CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
            if (outline == null) return;

            outline.SetOutlined(true, throughWalls: true);

            await UniTask.Delay(TimeSpan.FromSeconds(highlightDuration), cancellationToken: ct);
            if (outline != null)
                outline.SetOutlined(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (scanOrigin == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(scanOrigin.position, scanRadius);
    }
}