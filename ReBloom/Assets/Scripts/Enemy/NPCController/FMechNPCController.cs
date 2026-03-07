using Cysharp.Threading.Tasks;
using Unity.Netcode;
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


    [SerializeField] private DayNightCycle dayNightCycle;

    [SerializeField] private StageDetector playerStageDetector;

    private FMechNPCSound sound;

    private const int FxLaugh = 1;
    private const int FxLocalJumpscare = 2;

    protected override void Start()
    {
        base.Start();

        sound = GetComponentInChildren<FMechNPCSound>();
        
        // Player의 StageDetector 찾기
        // if (player != null)
        // {
        //     playerStageDetector = player.GetComponent<StageDetector>();
        //     if (playerStageDetector == null)
        //     {
        //         Debug.LogError("[F-Mech] Player에서 StageDetector를 찾을 수 없습니다!");
        //     }

        //     dayNightCycle = player.GetComponent<DayNightCycle>();
        // }
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
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

        if (!HasServerAuthority)
            return;

        CheckKillDistance();
        
        // 플레이어 스테이지 변경 감지
        if (GetObservedTargetStageId() > 0)
        {
            int currentStageID = GetObservedTargetStageId();

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


    protected override void RefreshTargetPlayer(bool force = false)
    {
        if (isPlayingJumpscare && playerController != null)
            return;

        base.RefreshTargetPlayer(force);
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

    protected override void HandleFxEvent(int fxEventId, Vector3 position, Vector3 direction, float value)
    {
        switch (fxEventId)
        {
            case FxLaugh:
                sound?.PlayLaugh();
                break;
            case FxLocalJumpscare:
                PlayJumpscareAsync(this.GetCancellationTokenOnDestroy()).Forget();
                break;
        }
    }

    public bool IsPlayerLookingAt()
    {
        if (player == null) return false;
        if (!TryGetObservedCameraPose(out Vector3 cameraPosition, out Vector3 cameraForward)) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > maxDetectionDistance) return false;
        if (distance < 2f) return false;

        Vector3 directionToNPC = (transform.position - cameraPosition).normalized;
        float angle = Vector3.Angle(cameraForward, directionToNPC);

        if (angle <= detectionAngle)
        {
            Debug.Log("[F-Mech] 플레이어 카메라 NPC 룩");
            return true;
        }

        return false;
    }

    public bool IsPlayerInMyStage()
    {
        int stageId = GetObservedTargetStageId();
        return stageId > 0 && stageId == myStageID;
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
        if (isPlayingJumpscare) return;

        if (!IsNetworkedSession)
        {
            if (playerController == null) return;
            PlayJumpscareAsync(this.GetCancellationTokenOnDestroy()).Forget();
            return;
        }

        if (!TryGetTargetClientId(out ulong clientId))
            return;

        isPlayingJumpscare = true;
        BroadcastFxEvent(FxLaugh);
        BroadcastFxEventToClient(clientId, FxLocalJumpscare);
        PlayNetworkedJumpscareServerAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid PlayNetworkedJumpscareServerAsync(CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(jumpscareDuration), cancellationToken: ct);
        }
        catch (System.OperationCanceledException)
        {
            isPlayingJumpscare = false;
            return;
        }

        NetworkPlayerOwnerGate targetGate = GetTargetGate();
        if (targetGate != null)
            targetGate.ApplyAuthoritativeDamage(100f);

        isPlayingJumpscare = false;

        if (HasServerAuthority)
            ChangeState(new FMechNPCReturnState(this));
    }

    private async UniTask PlayJumpscareAsync(CancellationToken ct)
    {
        PlayerController effectPlayer = ResolveJumpscarePlayerController();
        if (effectPlayer == null) return;
        if (isPlayingJumpscare) return;

        isPlayingJumpscare = true;

        if (effectPlayer.playerStats != null)
        {
            effectPlayer.playerStats.SetInvincible(true);
        }

        SoundManager.I?.PlaySurprise();
        effectPlayer.SetBlocked(true);

        Vector3 frozenPosition = transform.position;

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
            mainCamera.transform.SetParent(null);
            mainCamera.transform.position = jumpscarePosition.position;
            mainCamera.transform.rotation = jumpscarePosition.rotation;

            Debug.Log("[F-Mech] Jumpscare 시작!");
        }

        try
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(jumpscareDuration), cancellationToken: ct);

            mainCamera.transform.SetParent(originalParent);
            mainCamera.transform.position = originalPosition;
            mainCamera.transform.rotation = originalRotation;

            if (tpCam != null && wasTPCamEnabled)
            {
                tpCam.enabled = true;
            }

            transform.position = frozenPosition;

            if (agent != null && wasAgentEnabled)
            {
                agent.enabled = true;
            }

            if (animator != null && wasAnimatorEnabled)
            {
                animator.enabled = true;
            }

            Debug.Log("[F-Mech] Jumpscare 종료");
            if (effectPlayer.playerStats != null)
            {
                effectPlayer.playerStats.SetInvincible(false);
            }

            if (!IsNetworkedSession && HasServerAuthority)
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

            if (agent != null && wasAgentEnabled)
            {
                agent.enabled = true;

                if (agent.isOnNavMesh)
                {
                    agent.Warp(frozenPosition);
                }
                else
                {
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(frozenPosition, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        agent.Warp(hit.position);

                        Debug.Log("[F-Mech] Jumpscare 취소됨");
                    }
                }
            }

            if (animator != null && wasAnimatorEnabled)
            {
                animator.enabled = true;
            }

            if (effectPlayer.playerStats != null)
                effectPlayer.playerStats.SetInvincible(false);

            effectPlayer.SetBlocked(false);
        }
        finally
        {
            if (!effectPlayer.isDead)
                effectPlayer.SetBlocked(false);

            isPlayingJumpscare = false;
        }
    }

    private PlayerController ResolveJumpscarePlayerController()
    {
        if (!IsNetworkedSession)
            return playerController;

        NetworkPlayerOwnerGate[] gates = FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
        foreach (NetworkPlayerOwnerGate gate in gates)
        {
            if (gate != null && gate.IsOwner)
                return gate.GetComponent<PlayerController>();
        }

        return null;
    }

    private NetworkPlayerOwnerGate GetTargetGate()
    {
        return playerController != null ? playerController.GetComponent<NetworkPlayerOwnerGate>() : null;
    }

    private int GetObservedTargetStageId()
    {
        if (!IsNetworkedSession)
            return playerStageDetector != null && playerStageDetector.CurrentStage != null
                ? playerStageDetector.CurrentStage.StageID
                : -1;

        NetworkPlayerOwnerGate gate = GetTargetGate();
        return gate != null ? gate.CurrentStageId.Value : -1;
    }

    private bool TryGetObservedCameraPose(out Vector3 cameraPosition, out Vector3 cameraForward)
    {
        if (!IsNetworkedSession)
        {
            if (playerCamera == null)
            {
                cameraPosition = default;
                cameraForward = default;
                return false;
            }

            cameraPosition = playerCamera.transform.position;
            cameraForward = playerCamera.transform.forward;
            return true;
        }

        NetworkPlayerOwnerGate gate = GetTargetGate();
        if (gate == null)
        {
            cameraPosition = default;
            cameraForward = default;
            return false;
        }

        cameraPosition = gate.CameraPosition.Value;
        cameraForward = gate.CameraForward.Value;
        return cameraForward.sqrMagnitude > 0.01f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

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












