using UnityEngine;

public class GreenhouseUpgradeTerminal : BuildingInteractableBase
{
    private float holdTime = 1f;
    public override float HoldTime => holdTime;

    private ArcData arcData;
    private GreenhouseContext ctx;

    private void Start()
    {
        arcData = BuildManager.I.ArcDB.TryGet(building.arcId, out var data) ? data : null;
        holdTime = (arcData != null && arcData.interactTime > 0f) ? arcData.interactTime : 1.0f;

        ctx = GetComponentInParent<GreenhouseContext>();
        if (ctx == null)
            Debug.LogError("[GreenhouseUpgradeTerminal] GreenhouseContext not found in parents.");
    }

    public override void Interact(PlayerController player)
    {
        var provider = FarmPrefabProvider.I;
        if (provider == null)
        {
            Debug.Log("FarmPrefabProvider가 없습니다.");
            return;
        }

        if (ctx == null)
        {
            Debug.LogError("[GreenhouseUpgradeTerminal] GreenhouseContext가 없습니다.");
            return;
        }

        var db = provider.GreenhouseUpgradeDB;
        ctx.ApplyUpgradeStateLocally(false);

        var ui = UIManager.Instance.GetUI<GreenhouseUpgradeUI>(UIType.FarmUpgrade);
        ui.Open(ctx, db, player != null ? player.Inventory?.Data : null);
    }
}
