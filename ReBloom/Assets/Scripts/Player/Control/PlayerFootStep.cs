using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerFootstep : NetworkBehaviour
{
    public static event Action<Vector3, float> OnFootstep;
    [SerializeField] private float stepInterval = 0.5f;
    private float stepTimer = 0f;

    private PlayerController playerController;
    private StageDetector stageDetector;

    private bool IsNetworkedSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        stageDetector = GetComponent<StageDetector>();
    }

    private void Start()
    {
        if (stageDetector == null) stageDetector = StageDetector.I;
    }

    private void Update()
    {
        if (!ShouldEmitFootsteps())
            return;

        if (playerController.currentSpeed > 0.1f)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval && stageDetector.CurrentStage.stageID != 400)
            {
                stepTimer = 0f;
                float loudness = playerController.isSlow ? 0.3f : 1.0f;
                EmitFootstep(transform.position, loudness);
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private bool ShouldEmitFootsteps()
    {
        if (!IsNetworkedSession)
            return true;

        return IsSpawned && IsOwner;
    }

    private void EmitFootstep(Vector3 position, float loudness)
    {
        if (!IsNetworkedSession)
        {
            OnFootstep?.Invoke(position, loudness);
            return;
        }

        if (IsServer)
        {
            OnFootstep?.Invoke(position, loudness);
            return;
        }

        ReportFootstepServerRpc(position, loudness);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportFootstepServerRpc(Vector3 position, float loudness)
    {
        OnFootstep?.Invoke(position, loudness);
    }
}
