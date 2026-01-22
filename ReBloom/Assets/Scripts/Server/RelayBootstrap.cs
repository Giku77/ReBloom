using TMPro;
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
    [SerializeField] private float connectTimeout = 10f;

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
                if (ToastService.I != null) ToastService.I.Show("서버에 접속 중입니다...", 30000);
            });
    }

    public async void StartHostRelay(int maxConnections = 10, string connectionType = "dtls")
    {
        var alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(alloc, connectionType));

        nm.StartHost();
        Debug.Log($"[Relay] Join Code: {joinCode}");
        JoinCodeStore.Current = joinCode;
        nm.SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);
    }


    public async void StartClientRelay(string joinCode, string connectionType = "dtls")
    {
        try
        {
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            utp.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, connectionType));

            if (!nm.StartClient())
            {
                FailJoin("클라이언트 시작 실패");
                return;
            }

            nm.StartClient();

            float end = Time.unscaledTime + connectTimeout;
            while (Time.unscaledTime < end && !nm.IsConnectedClient)
                await System.Threading.Tasks.Task.Yield();

            if (!nm.IsConnectedClient)
            {
                FailJoin("서버 접속 시간 초과");
                nm.Shutdown();
            }
        }
        catch (RelayServiceException e)
        {
            // 조인코드 잘못됨 / 릴레이 오류 등
            FailJoin($"참여 실패: {e.Reason}");
        }
        catch (System.Exception e)
        {
            FailJoin($"참여 실패: {e.Message}");
        }
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
        Debug.Log($"[Net] ClientConnected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"[Net] ClientDisconnected: {clientId}");
    }

    private void FailJoin(string msg)
    {
        if (ToastService.I != null)
            ToastService.I.Show(msg);

        Debug.LogError($"[Net] {msg}");
    }



}
