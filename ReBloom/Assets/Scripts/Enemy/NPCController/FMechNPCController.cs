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


    private float detectionAngle = 30f;
    private float maxDetectionDistance = 30f;

    [Header("Jumpscare Settings")]
    public Transform jumpscarePosition; // F-Mech 얼굴 앞 위치
    private float jumpscareDuration = 2f;

    public bool isPlayingJumpscare = false;

    [Header("Kill Settings")]
    public float killDistance = 1.5f;

    [Header("Speed Settings")]
    public float chaseSpeed = 3f;
    public float returnSpeed = 5f;

    [Header("SpawnPoints")]
    [SerializeField] Transform schoolSpawnPoint;
    [SerializeField] Transform storeSpawnPoint;
    [SerializeField] Transform factorySpawnPoint;


    private DayNightCycle dayNightCycle;

    private StageDetector playerStageDetector;

    private FMechNPCSound sound;

    protected override void Start()
    {
        base.Start();

        sound = GetComponentInChildren<FMechNPCSound>();
        
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
        
        // 플레이어 스테이지 변경 감지
        if (playerStageDetector != null && playerStageDetector.CurrentStage != null)
        {
            int currentStageID = playerStageDetector.CurrentStage.StageID;
            
            // 새 스테이지로 진입했고, F-Mech가 활동하는 스테이지면
            if (currentStageID != lastDetectedStageID && 
                (currentStageID == 401 || currentStageID == 402 || currentStageID == 403))
            {
                WarpToStage(currentStageID);
                lastDetectedStageID = currentStageID;
                
                // Idle 상태로 리셋
                ChangeState(new FMechNPCIdleState(this));
            }
        }
    }

    protected override void UpdateAnimation()
    {
        if (isPlayingJumpscare) return;

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

        if (distance < 2f) return false;

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

    private async UniTask PlayJumpscareAsync(CancellationToken ct)
    {
        isPlayingJumpscare = true;

        if (playerController?.playerStats != null)
        {
            playerController.playerStats.SetInvincible(true);
        }

        SoundManager.I?.PlaySurprise();
        sound.PlayLaugh();

        playerController.SetBlocked(true);
        
        // F-Mech 위치 고정
        Vector3 frozenPosition = transform.position;
        //Quaternion frozenRotation = transform.rotation;
        
        // NavMeshAgent 완전히 끄기
        bool wasAgentEnabled = false;
        if (agent != null)
        {
            wasAgentEnabled = agent.enabled;
            agent.enabled = false;
        }

        bool wasAnimatorEnabled = false;
        if (animator != null)
        {
            wasAnimatorEnabled = animator.enabled;
            animator.enabled = false;
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
            //transform.rotation = frozenRotation;
            
            if (agent != null && wasAgentEnabled)
            {
                agent.enabled = true;
            }

            if (animator != null && wasAnimatorEnabled)
            {
                animator.enabled = true;
            }

            Debug.Log("[F-Mech] Jumpscare 종료");

            if (playerController?.playerStats != null)
            {
                playerController.playerStats.SetInvincible(false);
                playerController.playerStats.TakeDamage(100);
            }

            // Return 상태로 전환
            ChangeState(new FMechNPCReturnState(this));
        }
        catch (System.OperationCanceledException)
        {
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
            
            transform.position = frozenPosition;
            //transform.rotation = frozenRotation;

            if (agent != null && wasAgentEnabled)
            {
                agent.enabled = true;

                // NavMesh 위로 다시 Warp
                if (agent.isOnNavMesh)
                {
                    agent.Warp(frozenPosition);
                }
                else
                {
                    // NavMesh 위에 없으면 가장 가까운 NavMesh 위치로
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(frozenPosition, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        agent.Warp(hit.position);


                        Debug.Log("[F-Mech] Jumpscare 취소됨");
                    }
                }

                if (animator != null && wasAnimatorEnabled)
                {
                    animator.enabled = true;
                }
            }
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

    public void WarpToStage(int stageID)
    {
        Transform targetSpawn = null;
        
        switch (stageID)
        {
            case 401: // 학교
                targetSpawn = schoolSpawnPoint;
                break;
            case 402: // 백화점
                targetSpawn = storeSpawnPoint;
                break;
            case 403: // 공장
                targetSpawn = factorySpawnPoint;
                break;
            default:
                Debug.LogWarning($"[F-Mech] 알 수 없는 스테이지 ID: {stageID}");
                return;
        }
        
        if (targetSpawn == null)
        {
            Debug.LogError($"[F-Mech] 스테이지 {stageID}의 스폰 포인트가 설정되지 않았습니다!");
            return;
        }
        
        // 현재 스테이지 ID 업데이트
        myStageID = stageID;
        
        // 위치 워프
        transform.position = targetSpawn.position;
        transform.rotation = targetSpawn.rotation;
        
        // initialPosition 업데이트 (Return 상태에서 돌아갈 위치)
        initialPosition = targetSpawn.position;
        initialRotation = targetSpawn.rotation;
        
        // NavMeshAgent Warp
        if (agent != null && agent.enabled)
        {
            agent.Warp(targetSpawn.position);
        }
        
        Debug.Log($"[F-Mech] 스테이지 {stageID}로 워프 완료!");
    }

    private int lastDetectedStageID = -1;

    public bool IsNightTime()
    {
        if (dayNightCycle == null) return false;
        return dayNightCycle.IsNightTime();
    }
}