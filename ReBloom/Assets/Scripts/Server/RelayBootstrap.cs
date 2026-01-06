using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class RelayBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager nm;
    [SerializeField] private UnityTransport utp;

    [SerializeField] private Button HostButton;
    [SerializeField] private Button ClientButton;

    private async void Awake()
    {
        if (!nm) nm = NetworkManager.Singleton;
        if (!utp) utp = nm.GetComponent<UnityTransport>();

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        if (HostButton)
            HostButton.onClick.AddListener(() => StartHostRelay());
        if (ClientButton)
            ClientButton.onClick.AddListener(() => StartClientRelay("ENTER_JOIN_CODE_HERE"));
    }

    public async void StartHostRelay(int maxConnections = 3, string connectionType = "dtls")
    {
        var alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(alloc, connectionType));

        nm.StartHost();
        Debug.Log($"[Relay] Join Code: {joinCode}");
    }

    public async void StartClientRelay(string joinCode, string connectionType = "dtls")
    {
        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, connectionType));

        nm.StartClient();
    }
}
