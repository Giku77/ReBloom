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
        var dayNightCycle = DayNightCycle.Instance;
        if (dayNightCycle == null)
        {
            Debug.LogWarning("[SleepingPod] DayNightCycle instance not found.");
            return;
        }

        if (!dayNightCycle.IsNightTime())
        {
            ToastMessageUI.Instance.Show("수면 캡슐은 밤에만 사용할 수 있습니다.");
            return;
        }

        string successMessage = arcData != null ? arcData.interactText : "플레이어가 체력을 회복했습니다.";

        if (dayNightCycle.RequestCollectiveSleep(player, successMessage))
            return;

        StartSleepOffline(player, dayNightCycle, successMessage).Forget();
    }

    private async UniTask StartSleepOffline(PlayerController player, DayNightCycle dayNightCycle, string successMessage)
    {
        var effectUI = UIManager.Instance.GetUI<PlayerEffectUI>(UIType.PlayerEffect);
        if (effectUI == null)
        {
            Debug.LogError("[SleepingPod] PlayerEffectUI를 찾을 수 없습니다!");
            return;
        }

        player.SetBlocked(true);
        UIManager.Instance?.SetBlockingInput(true);

        await effectUI.FadeToBlack(1.5f);

        SoundManager.I?.PlayYawn();

        await UniTask.Delay(1500);

        var playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null)
            playerStats.Health.Set(100f);

        dayNightCycle.SleepUntilMorning();

        await UniTask.Delay(1000);

        ToastMessageUI.Instance.Show(successMessage);

        await effectUI.FadeFromBlack(1.5f);

        await UniTask.Delay(1000);

        player.SetBlocked(false);
        UIManager.Instance?.SetBlockingInput(false);
    }
}
