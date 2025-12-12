using UnityEngine;

public class SleepingPodInteractable : BuildingInteractableBase
{
    private float holdTime;
    public override float HoldTime => holdTime; 

    private ArcData arcData;

    private void Start()
    {
        arcData = BuildManager.I.ArcDB.TryGet(building.arcId, out var data) ? data : null;
        holdTime = arcData != null && arcData.interactTime > 0f ? arcData.interactTime : 1.0f;
    }

    public override void Interact(PlayerController player)
    {
        var dayNightCycle = player.GetComponent<DayNightCycle>();
        if (dayNightCycle != null && !dayNightCycle.IsNightTime())
        {
            ToastMessageUI.Instance.Show("수면 캡슐은 밤에만 사용할 수 있습니다.");
            return;
        }
        var playerstats = player.GetComponent<PlayerStats>();
        if (playerstats != null)
        {
            playerstats.Health.Set(100f);
            playerstats.Hunger.Set(0f);
            playerstats.Pollution.Set(0f);
            playerstats.Thirst.Set(0f);
            playerstats.Temperature.Set(36.5f);
        }
        ToastMessageUI.Instance.Show(arcData != null ? arcData.interactText : "플레이어가 체력을 회복했습니다.");
        if (dayNightCycle != null)
        {
            dayNightCycle.AdvanceHours(6f);
        }
    }
}
