using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Mathematics;
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
    [SerializeField] private float cooldown = 3f;

    public float Cooldown => cooldown;

    public float RemainingCooldown
    {
        get
        {
            return Mathf.Max(0f, (lastScanTime + cooldown) - Time.time);
        }
    }

    public float Cooldown01
    {
        get
        {
            if (cooldown <= 0f) return 0f;
            return RemainingCooldown / cooldown;
        }
    }

    public bool IsOnCooldown => RemainingCooldown > 0f;

    private float lastScanTime = -999f;
    private CancellationToken destroyToken;

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

    public void TriggerScan()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
        {
            return;
        }
        if (Time.time < lastScanTime + cooldown)
        {
            float remain = Mathf.Max(0f, (lastScanTime + cooldown) - Time.time);
            ToastMessageUI.Instance.Show($"스캔이 쿨타임중입니다. : {remain:F1}초");
            return;
        }

        lastScanTime = Time.time;

        if (scanOrigin == null)
        {
            Debug.LogWarning("[ScanController] scanOrigin not set");
            return;
        }

        animController?.PlayAnimation("Scan");

        SoundManager.I?.PlayScan();

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
