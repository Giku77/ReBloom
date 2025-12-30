using Cysharp.Threading.Tasks;
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

        StartSleep(player, dayNightCycle).Forget();


        //var playerstats = player.GetComponent<PlayerStats>();
        //if (playerstats != null)
        //{
        //    playerstats.Health.Set(100f);
        //    playerstats.Hunger.Set(0f);
        //    playerstats.Pollution.Set(0f);
        //    playerstats.Thirst.Set(0f);
        //    playerstats.Temperature.Set(36.5f);
        //}
        //ToastMessageUI.Instance.Show(arcData != null ? arcData.interactText : "플레이어가 체력을 회복했습니다.");
        //if (dayNightCycle != null)
        //{
        //    dayNightCycle.AdvanceHours(6f);
        //}
    }

    private async UniTask StartSleep(PlayerController player, DayNightCycle dayNightCycle)
    {
        var effectUI = UIManager.Instance.GetUI<PlayerEffectUI>(UIType.PlayerEffect);
        if (effectUI == null)
        {
            Debug.LogError("[SleepingPod] PlayerEffectUI를 찾을 수 없습니다!");
            return;
        }

        player.SetBlocked(true);
        UIManager.Instance?.SetBlockingInput(true);

        //player.Anim?.PlaySleep();

        await effectUI.FadeToBlack(1.5f);

        SoundManager.I?.PlayYawn();

        await UniTask.Delay(1500);

        var playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Health.Set(100f);
            playerStats.Hunger.Set(0f);
            playerStats.Pollution.Set(0f);
            playerStats.Thirst.Set(0f);
            playerStats.Temperature.Set(36.5f);
        }

        //player.Anim?.PlaySleep();

        if (dayNightCycle != null)
        {
            //dayNightCycle.AdvanceHours(6f);
            dayNightCycle.SleepUntilMorning();
        }

        await UniTask.Delay(1000);

        ToastMessageUI.Instance.Show(arcData != null ? arcData.interactText : "플레이어가 체력을 회복했습니다.");

        //player.Anim?.PlayStandUp();

        await effectUI.FadeFromBlack(1.5f);

        await UniTask.Delay(1000);

        player.SetBlocked(false);
        UIManager.Instance?.SetBlockingInput(false);
    }
}
