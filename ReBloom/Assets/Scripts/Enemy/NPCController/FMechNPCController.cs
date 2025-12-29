using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class FMechNPCController : BaseNPCController
{
    [Header("F-Mech Specific")]
    [Header("Territory Settings")]
    public int myStageID = 401;

    [Header("Vision Detection")]
    public Camera playerCamera;
    public float detectionAngle = 90f;
    public float maxDetectionDistance = 30f;

    [Header("Jumpscare Settings")]
    public Transform jumpscarePosition; // F-Mech 얼굴 앞 위치
    private float jumpscareDuration = 2f;
    private bool isPlayingJumpscare = false;

    [Header("Kill Settings")]
    public float killDistance = 1.5f;

    [Header("Speed Settings")]
    public float chaseSpeed = 3f;
    public float returnSpeed = 5f;

    private DayNightCycle dayNightCycle;

    private StageDetector playerStageDetector;

    protected override void Start()
    {
        base.Start();
        
        // Player의 StageDetector 찾기
        if (player != null)
        {
            playerStageDetector = player.GetComponent<StageDetector>();
            if (playerStageDetector == null)
            {
                Debug.LogError("[F-Mech] Player에서 StageDetector를 찾을 수 없습니다!");
            }

            dayNightCycle = player.GetComponent<DayNightCycle>();
        }
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        ChangeState(new FMechNPCIdleState(this));
        
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null)
        {
            Debug.Log($"[F-Mech] Collider 타입: {myCollider.GetType().Name}");
            Debug.Log($"[F-Mech] Is Trigger: {myCollider.isTrigger}");
            Debug.Log($"[F-Mech] Enabled: {myCollider.enabled}");
            
            if (myCollider.isTrigger)
            {
                Debug.LogWarning("[F-Mech] Collider가 Trigger로 설정되어 있지만 QueryTriggerInteraction.Collide로 감지합니다.");
            }
        }
        else
        {
            Debug.LogError("[F-Mech] Collider가 없습니다!");
        }
    }

    protected override void InitializeState()
    {
        ChangeState(new FMechNPCIdleState(this));
    }

    protected override void Update()
    {
        base.Update();
        CheckKillDistance();
    }

    protected override void UpdateAnimation()
    {
        if (animator != null && agent != null)
        {
            bool isMoving = !agent.isStopped && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
            float speed = isMoving ? agent.velocity.magnitude : 0f;
            animator.SetFloat("Speed", speed);
        }
    }

    public bool IsPlayerLookingAt()
    {
        if (player == null || playerCamera == null) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > maxDetectionDistance) return false;
        
        Vector3 directionToNPC = (transform.position - playerCamera.transform.position).normalized;
        
        float angle = Vector3.Angle(playerCamera.transform.forward, directionToNPC);
        
        if (angle <= detectionAngle)
        {
            Debug.Log($"[F-Mech] 플레이어 카메라 NPC 룩");
            return true;
        }
        
        return false;
    }

    public bool IsPlayerInMyStage()
    {
        if (playerStageDetector == null || playerStageDetector.CurrentStage == null)
            return false;

        return playerStageDetector.CurrentStage.StageID == myStageID;
    }

    private void CheckKillDistance()
    {
        if (player == null) return;

        if (currentState is FMechNPCChaseState || currentState is FMechNPCFrozenState)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= killDistance)
            {
                KillPlayer();
            }
        }
    }

    private void KillPlayer()
    {
        if (playerController == null) return;
        if (isPlayingJumpscare) return;

        PlayJumpscareAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

private async UniTaskVoid PlayJumpscareAsync(CancellationToken ct)
    {
        isPlayingJumpscare = true;
        
        // F-Mech 위치 고정
        Vector3 frozenPosition = transform.position;
        Quaternion frozenRotation = transform.rotation;
        
        // NavMeshAgent 완전히 끄기
        bool wasAgentEnabled = false;
        if (agent != null)
        {
            wasAgentEnabled = agent.enabled;
            agent.enabled = false;
        }
        
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[F-Mech] Main Camera를 찾을 수 없습니다!");
            isPlayingJumpscare = false;
            if (agent != null && wasAgentEnabled) agent.enabled = true;
            return;
        }
        
        // ThirdPersonCamera 비활성화
        ThirdPersonCamera tpCam = mainCamera.GetComponentInParent<ThirdPersonCamera>();
        bool wasTPCamEnabled = false;
        if (tpCam != null)
        {
            wasTPCamEnabled = tpCam.enabled;
            tpCam.enabled = false;
        }
        
        Vector3 originalPosition = mainCamera.transform.position;
        Quaternion originalRotation = mainCamera.transform.rotation;
        Transform originalParent = mainCamera.transform.parent;
        
        if (jumpscarePosition != null)
        {
            // 부모 해제하고 위치 이동
            mainCamera.transform.SetParent(null);
            mainCamera.transform.position = jumpscarePosition.position;
            mainCamera.transform.rotation = jumpscarePosition.rotation;
            
            Debug.Log("[F-Mech] Jumpscare 시작!");
        }
        
        try
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(jumpscareDuration), cancellationToken: ct);
            
            // 카메라 복구
            mainCamera.transform.SetParent(originalParent);
            mainCamera.transform.position = originalPosition;
            mainCamera.transform.rotation = originalRotation;
            
            if (tpCam != null && wasTPCamEnabled)
            {
                tpCam.enabled = true;
            }
            
            // F-Mech 위치 복원 및 Agent 재활성화
            transform.position = frozenPosition;
            transform.rotation = frozenRotation;
            
            if (agent != null && wasAgentEnabled)
            {
                agent.enabled = true;
            }
            
            Debug.Log("[F-Mech] Jumpscare 종료");
            
            // 플레이어 데미지
            playerController.playerStats.TakeDamage(100);
            
            // Return 상태로 전환
            ChangeState(new FMechNPCReturnState(this));
        }
        catch (System.OperationCanceledException)
        {
            // 복구
            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(originalParent);
                mainCamera.transform.position = originalPosition;
                mainCamera.transform.rotation = originalRotation;
                
                if (tpCam != null && wasTPCamEnabled)
                {
                    tpCam.enabled = true;
                }
            }
            
            // F-Mech 복구
            transform.position = frozenPosition;
            transform.rotation = frozenRotation;
            
            if (agent != null && wasAgentEnabled)
            {
                agent.enabled = true;
            }
            
            Debug.Log("[F-Mech] Jumpscare 취소됨");
        }
        finally
        {
            isPlayingJumpscare = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentState is FMechNPCChaseState || currentState is FMechNPCFrozenState)
            {
                KillPlayer();
            }
        }
    }

    public bool IsNightTime()
    {
        if (dayNightCycle == null) return false;
        return dayNightCycle.IsNightTime();
    }
}