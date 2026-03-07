using Unity.Netcode;
using UnityEngine;

public class PlayerSaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private NetworkObject networkObject;

    public string EntityGuid => "player";

    private void Reset()
    {
        stats = GetComponent<PlayerStats>();
        networkObject = GetComponent<NetworkObject>();
    }

    private bool ShouldHandleSave()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        if (networkObject == null)
            networkObject = GetComponent<NetworkObject>();

        return networkObject != null && networkObject.IsOwner && NetworkManager.Singleton.IsServer;
    }

    public void Capture(SaveGameDTO save)
    {
        if (save == null || !ShouldHandleSave()) return;

        save.player.transform = TransformDTO.From(transform);

        if (stats == null) return;

        save.player.hp = stats.Health.Value;
        save.player.hunger = stats.Hunger.Value;
        save.player.thirst = stats.Thirst.Value;
        save.player.pollution = stats.Pollution.Value;
        save.player.temperature = stats.Temperature.Value;
    }

    public void Restore(SaveGameDTO save)
    {
        if (save == null || !ShouldHandleSave()) return;

        save.player.transform?.ApplyTo(transform);

        if (stats == null) return;

        stats.Health.Set(save.player.hp);
        stats.Hunger.Set(save.player.hunger);
        stats.Thirst.Set(save.player.thirst);
        stats.Pollution.Set(save.player.pollution);
        stats.Temperature.Set(save.player.temperature);
    }
}
