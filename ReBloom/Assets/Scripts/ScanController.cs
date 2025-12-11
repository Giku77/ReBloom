using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScanController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform scanOrigin;                 // 보통 플레이어 Transform
    [SerializeField] private SimpleSonarShader_Object sonarSource; // 씬 어딘가에 있는 SimpleSonarShader_Object
    [SerializeField] private LayerMask interactableMask;           // 상호작용 오브젝트 레이어
    [SerializeField] private InputAction scanAction;                  // 스캔 입력 액션
    [SerializeField] private RobotAnimationController animController;

    [Header("Scan Settings")]
    [SerializeField] private float scanRadius = 15f;      // 스캔 최대 거리 (= intensity)
    [SerializeField] private float ringSpeed = 10f;       // 셰이더 _RingSpeed 와 맞춰주면 좋음
    [SerializeField] private float highlightDuration = 2f;// 아웃라인 유지 시간
    [SerializeField] private float cooldown = 3f;

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
        if (Time.time < lastScanTime + cooldown)
            return;

        lastScanTime = Time.time;

        if (scanOrigin == null || sonarSource == null)
        {
            Debug.LogWarning("[ScanController] scanOrigin 또는 sonarSource 가 설정 안 됨");
            return;
        }
        
        animController?.PlayAnimation("Scan");

        Vector3 origin = scanOrigin.position;

        var pos4 = new Vector4(origin.x, origin.y, origin.z, 0f);
        sonarSource.StartSonarRing(pos4, scanRadius);

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
            // 씬 전환/오브젝트 파괴 시 조용히 무시
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (scanOrigin == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(scanOrigin.position, scanRadius);
    }
}
