using Unity.Netcode;
using UnityEngine;

public class PlayerNameSubmitter : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        string myName = NicknameStore.CurrentName;

        SubmitWhenReady(myName);
    }

    private async void SubmitWhenReady(string myName)
    {
        while (PlayerRegistry.I == null)
            await System.Threading.Tasks.Task.Yield();

        PlayerRegistry.I.SubmitNameRpc(NetworkManager.Singleton.LocalClientId, myName);
    }
}
