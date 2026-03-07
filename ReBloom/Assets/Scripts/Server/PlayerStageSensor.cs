using Unity.Netcode;
using UnityEngine;

public class PlayerStageSensor : NetworkBehaviour
{
    private StageService stageService;

    private void Awake()
    {
        stageService = StageService.I != null
            ? StageService.I
            : FindFirstObjectByType<StageService>();
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsOwner; // 로컬만 센서 동작
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;

        if (other.TryGetComponent<StageBase>(out var stage))
        {
            stageService?.SetStage(stage);
            NetworkQuestManager.I?.ReportEnter(stage.StageID);
        }

        int layer = other.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Inside"))
            stageService?.SetInside(true);
        else if (layer == LayerMask.NameToLayer("Outside"))
            stageService?.SetInside(false);

        if (layer == LayerMask.NameToLayer("Buildable"))
            stageService?.SetCanBuild(true);
        else if (layer == LayerMask.NameToLayer("Unbuildable"))
            stageService?.SetCanBuild(false);
    }
}
