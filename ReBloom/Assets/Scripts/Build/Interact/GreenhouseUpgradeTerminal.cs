using UnityEngine;

public class GreenhouseUpgradeTerminal : BuildingInteractableBase
{
    private float holdTime = 1f;
    public override float HoldTime => holdTime;

    private ArcData arcData;

    private GreenhouseContext ctx;

    private void Start()
    {
        // 1) ArcData 기반 holdTime (SleepingPod랑 동일 패턴)
        arcData = BuildManager.I.ArcDB.TryGet(building.arcId, out var data) ? data : null;
        holdTime = (arcData != null && arcData.interactTime > 0f) ? arcData.interactTime : 1.0f;

        // 2) 온실 컨텍스트 찾기 (온실 프리팹 내부에서 monitor가 있으니 Parent에서 찾는 게 정석)
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

        var db = provider.GreenhouseUpgradeDB;

        // TODO: 세이브 로드
        // var state = SaveManager.I.LoadGreenhouseUpgradeState(ctx.Id) ?? new GreenhouseUpgradeState { greenhouseId = ctx.Id };

        var state = provider.GetOrCreateUpgradeState(ctx.Id);

        GreenhouseUpgradeService.ApplyAllSaved(ctx, state, db);
        var ui = UIManager.Instance.GetUI<GreenhouseUpgradeUI>(UIType.FarmUpgrade);
        ui.Open(ctx, state, db, player.Inventory.Data);
    }

}
