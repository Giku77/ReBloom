using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager nm;
    [SerializeField] private UnityTransport utp;

    [SerializeField] private Button HostButton;
    [SerializeField] private Button ClientButton;
    [SerializeField] private GameObject JoinPanel;
    [SerializeField] private TMPro.TMP_InputField JoinCodeInput;
    [SerializeField] private TMPro.TMP_InputField NameTagInput;
    [SerializeField] private Button JoinConfirmButton;
    [SerializeField] private Button JoinCancelButton;

    private async void Awake()
    {
        if (!nm) nm = NetworkManager.Singleton;
        if (!utp) utp = nm.GetComponent<UnityTransport>();

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        SetupApproval();
        if (HostButton)
        {
            HostButton.onClick.AddListener(() =>
            {
                NicknameStore.CurrentName = "방장";
                StartHostRelay();
            });
        }

        if (JoinCodeInput)
        {
            JoinCodeInput.onValueChanged.AddListener(v =>
            {
                var up = v.ToUpperInvariant();
                if (up != v)
                    JoinCodeInput.SetTextWithoutNotify(up);
            });
        }
        if (ClientButton)
        {
            ClientButton.onClick.AddListener(() =>
            {
                if (JoinPanel) JoinPanel.SetActive(true);
                else return;
            });
        }
        if (JoinCancelButton)
            JoinCancelButton.onClick.AddListener(() => {
                if (JoinPanel) JoinPanel.SetActive(false);
            });
        if (JoinConfirmButton)
            JoinConfirmButton.onClick.AddListener(() => {
                NicknameStore.CurrentName = NameTagInput ? NameTagInput.text : "Player";
                string joinCode = JoinCodeInput ? JoinCodeInput.text : "";
                StartClientRelay(joinCode);
                if (JoinPanel) JoinPanel.SetActive(false);
            });
    }

    public async void StartHostRelay(int maxConnections = 3, string connectionType = "dtls")
    {
        var alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(alloc, connectionType));

        nm.StartHost();
        Debug.Log($"[Relay] Join Code: {joinCode}");
        nm.SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);
    }

    public async void StartClientRelay(string joinCode, string connectionType = "dtls")
    {
        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, connectionType));

        nm.StartClient();
        Debug.Log($"[Net] After StartClient: IsClient={nm.IsClient}, IsConnectedClient={nm.IsConnectedClient}, IsListening={nm.IsListening}");
    }

    private void SetupApproval()
    {
        var nm = NetworkManager.Singleton;
        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = Approval;
    }

    private void Approval(NetworkManager.ConnectionApprovalRequest req,
                        NetworkManager.ConnectionApprovalResponse res)
    {
        res.Approved = true;
        res.CreatePlayerObject = false; 
        res.Pending = false;
    }

    private void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        var nm = NetworkManager.Singleton;
        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Net] ClientConnected: {clientId} | IsHost={NetworkManager.Singleton.IsHost} IsServer={NetworkManager.Singleton.IsServer} IsClient={NetworkManager.Singleton.IsClient}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"[Net] ClientDisconnected: {clientId} | IsHost={NetworkManager.Singleton.IsHost} IsServer={NetworkManager.Singleton.IsServer} IsClient={NetworkManager.Singleton.IsClient}");
    }

}
