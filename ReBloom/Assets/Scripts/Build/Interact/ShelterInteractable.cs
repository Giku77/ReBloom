public class ShelterInteractable : BuildingInteractableBase
{
    public override float HoldTime => 1.0f; 

    public override void Interact(PlayerController player)
    {
        var playerstats = player.GetComponent<PlayerStats>();
        if (playerstats != null)
        {
            playerstats.Health.Set(100f);
            playerstats.Hunger.Set(0f);
            playerstats.Pollution.Set(0f);
            playerstats.Thirst.Set(0f);
            playerstats.Temperature.Set(36.5f);
        }
        toastMessageUI.Show("잠시 휴식을 취했습니다.");
    }
}
