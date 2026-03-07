using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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
    [SerializeField] private HostRoomPopupUI hostRoomPopup;
    [SerializeField] private GameObject JoinPanel;
    [SerializeField] private TMP_InputField JoinCodeInput;
    [SerializeField] private TMP_InputField NameTagInput;
    [SerializeField] private TMP_InputField PasswordInput;
    [SerializeField] private TextMeshProUGUI NameGuideText;
    [SerializeField] private TextMeshProUGUI PasswordGuideText;
    [SerializeField] private string defaultNameGuideText = "참여할 닉네임을 입력해주세요.";
    [SerializeField] private string defaultPasswordGuideText = "접속할 계정 비밀번호를 입력해주세요.";
    [SerializeField] private string invalidNameGuideText = "닉네임은 2자 이상 입력해주세요.";
    [SerializeField] private string invalidPasswordGuideText = "비밀번호를 입력해주세요.";
    [SerializeField] private Button JoinConfirmButton;
    [SerializeField] private Button JoinCancelButton;
    [SerializeField] private float connectTimeout = 30f;

    private Task servicesInitializationTask;
    private bool waitingForLocalClientConnection;
    private bool localClientConnected;
    private bool localClientDisconnected;
    private string lastDisconnectReason;

    private void Awake()
    {
        PrepareNetworkManagerForStart();
        BindUiEvents();
        servicesInitializationTask = InitializeServicesAsync();
        SetJoinGuideTexts(defaultNameGuideText, defaultPasswordGuideText);
    }

    private void OnEnable()
    {
        PrepareNetworkManagerForStart();
        if (nm == null)
            return;

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (nm == null)
            return;

        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void BindUiEvents()
    {
        if (HostButton)
        {
            HostButton.onClick.AddListener(() =>
            {
                if (hostRoomPopup != null)
                    hostRoomPopup.Open();
                else
                    FailJoin("호스트 팝업이 연결되지 않았습니다.");
            });
        }

        if (JoinCodeInput)
        {
            JoinCodeInput.onValueChanged.AddListener(v =>
            {
                var upper = v.ToUpperInvariant();
                if (upper != v)
                    JoinCodeInput.SetTextWithoutNotify(upper);
            });
        }

        if (ClientButton)
        {
            ClientButton.onClick.AddListener(() =>
            {
                if (JoinPanel) JoinPanel.SetActive(true);
                SetJoinGuideTexts(defaultNameGuideText, defaultPasswordGuideText);
            });
        }

        if (JoinCancelButton)
        {
            JoinCancelButton.onClick.AddListener(() =>
            {
                if (JoinPanel) JoinPanel.SetActive(false);
            });
        }

        if (JoinConfirmButton)
            JoinConfirmButton.onClick.AddListener(() => OnJoinConfirmClicked().Forget());
    }

    private async UniTaskVoid OnJoinConfirmClicked()
    {
        if (!TryGetJoinCredential(out var displayName, out var password))
        {
            FocusInvalidJoinField();
            return;
        }

        NicknameStore.CurrentName = displayName;
        string joinCode = JoinCodeInput ? JoinCodeInput.text : string.Empty;
        StartClientRelay(joinCode, displayName, password);
        if (JoinPanel) JoinPanel.SetActive(false);
        if (ToastService.I != null) ToastService.I.Show("서버에 접속 중입니다...", 30000);
    }

    private async Task InitializeServicesAsync()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async Task EnsureServicesReadyAsync()
    {
        if (servicesInitializationTask == null)
            servicesInitializationTask = InitializeServicesAsync();

        await servicesInitializationTask;
    }

    public void StartHostRelayWithCredential(string displayName, string password, int maxConnections = 10, string connectionType = "dtls")
    {
        if (!PlayFabAuth.TryValidateCredentials(displayName, password, out var normalizedDisplayName, out var normalizedPassword))
        {
            FailJoin("닉네임과 비밀번호를 모두 입력해주세요.");
            return;
        }

        NicknameStore.CurrentName = normalizedDisplayName;
        StartHostRelay(normalizedDisplayName, normalizedPassword, maxConnections, connectionType);
    }

    public async void StartHostRelay(string displayName, string password, int maxConnections = 10, string connectionType = "dtls")
    {
        try
        {
            await EnsureServicesReadyAsync();
            if (!await EnsureNetworkManagerStoppedAsync())
            {
                FailJoin("이전 세션 정리 중입니다. 잠시 후 다시 시도해주세요.");
                return;
            }

            PrepareNetworkManagerForStart();
            if (!await EnsurePlayFabAccountReadyAsync(displayName, password))
                return;

            var alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            utp.SetRelayServerData(AllocationUtils.ToRelayServerData(alloc, connectionType));

            SetupApproval();
            Debug.Log($"[Relay] Starting host. timeout={nm.NetworkConfig.ClientConnectionBufferTimeout}s sceneTimeout={nm.NetworkConfig.LoadSceneTimeOut}s autoSpawn={nm.NetworkConfig.AutoSpawnPlayerPrefabClientSide} approvalEnabled={nm.NetworkConfig.ConnectionApproval}");

            if (!nm.StartHost())
            {
                FailJoin("호스트 시작 실패");
                return;
            }

            Debug.Log($"[Relay] Join Code: {joinCode}");
            JoinCodeStore.Current = joinCode;
            hostRoomPopup?.CloseImmediate();
            nm.SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            FailJoin($"방 생성 실패: {e.Message}");
        }
    }

    public async void StartClientRelay(string joinCode, string displayName, string password, string connectionType = "dtls")
    {
        try
        {
            await EnsureServicesReadyAsync();
            if (!await EnsureNetworkManagerStoppedAsync())
            {
                FailJoin("이전 세션 정리 중입니다. 잠시 후 다시 시도해주세요.");
                return;
            }

            PrepareNetworkManagerForStart();
            if (!await EnsurePlayFabAccountReadyAsync(displayName, password))
                return;

            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            utp.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, connectionType));

            ResetClientConnectState();
            SetupApproval();
            Debug.Log($"[Relay] Starting client joinCode={joinCode} timeout={connectTimeout:F1}s approvalEnabled={nm.NetworkConfig.ConnectionApproval}");

            if (!nm.StartClient())
            {
                FailJoin("클라이언트 시작 실패");
                return;
            }

            LoadClientLoadingSceneIfNeeded();
            waitingForLocalClientConnection = true;
            float end = Time.unscaledTime + Mathf.Max(5f, connectTimeout);

            while (Time.unscaledTime < end)
            {
                if (nm.IsConnectedClient || localClientConnected)
                {
                    waitingForLocalClientConnection = false;
                    return;
                }

                if (localClientDisconnected)
                {
                    waitingForLocalClientConnection = false;
                    string reason = string.IsNullOrWhiteSpace(lastDisconnectReason) ? "서버와 연결할 수 없습니다." : lastDisconnectReason;
                    FailJoin($"서버 접속 실패: {reason}");
                    if (nm.IsListening)
                        ShutdownAndReturnClientToTitle();
                    return;
                }

                await Task.Yield();
            }

            waitingForLocalClientConnection = false;
            FailJoin("서버 접속 시간 초과");
            if (nm.IsListening)
                ShutdownAndReturnClientToTitle();
        }
        catch (RelayServiceException e)
        {
            FailJoin($"참여 실패: {e.Reason}");
        }
        catch (Exception e)
        {
            FailJoin($"참여 실패: {e.Message}");
        }
    }

    private async UniTask<bool> EnsureNetworkManagerStoppedAsync()
    {
        PrepareNetworkManagerForStart();

        if (nm == null)
            return false;

        if (nm.IsListening)
        {
            Debug.LogWarning("[Relay] Previous session is still listening. Shutting down before restart.");
            nm.Shutdown();
        }

        const float timeoutSeconds = 10f;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            if (!nm.IsListening && !nm.ShutdownInProgress)
                return true;

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield();
        }

        Debug.LogWarning($"[Relay] Network cleanup wait timed out. listening={nm.IsListening} shutdownInProgress={nm.ShutdownInProgress}");
        return !nm.IsListening && !nm.ShutdownInProgress;
    }

    private async UniTask<bool> EnsurePlayFabAccountReadyAsync(string displayName, string password)
    {
        if (!PlayFabAuth.TryValidateCredentials(displayName, password, out var normalizedDisplayName, out var normalizedPassword))
        {
            FailJoin("닉네임과 비밀번호를 모두 입력해주세요.");
            return false;
        }

        NicknameStore.CurrentName = normalizedDisplayName;

        try
        {
            if (SaveManager.I != null)
                return await SaveManager.I.EnsureRemoteReadyAsync(normalizedDisplayName, normalizedPassword);

            await PlayFabAuth.LoginAsync(normalizedDisplayName, normalizedPassword);
            return true;
        }
        catch (Exception e)
        {
            FailJoin($"계정 로그인 실패: {e.Message}");
            return false;
        }
    }

    private bool TryGetJoinCredential(out string displayName, out string password)
    {
        string rawDisplayName = NameTagInput ? NameTagInput.text : string.Empty;
        string rawPassword = PasswordInput ? PasswordInput.text : string.Empty;
        bool valid = PlayFabAuth.TryValidateCredentials(rawDisplayName, rawPassword, out displayName, out password);

        if (valid)
        {
            SetJoinGuideTexts(defaultNameGuideText, defaultPasswordGuideText);
            return true;
        }

        string normalizedDisplayName = PlayFabAuth.NormalizeDisplayName(rawDisplayName);
        if (normalizedDisplayName.Length < 2)
            SetJoinGuideTexts(invalidNameGuideText, defaultPasswordGuideText);
        else
            SetJoinGuideTexts(defaultNameGuideText, invalidPasswordGuideText);

        return false;
    }

    private void FocusInvalidJoinField()
    {
        string normalizedDisplayName = PlayFabAuth.NormalizeDisplayName(NameTagInput ? NameTagInput.text : string.Empty);
        if (normalizedDisplayName.Length < 2)
            NameTagInput?.ActivateInputField();
        else
            PasswordInput?.ActivateInputField();
    }

    private void SetJoinGuideTexts(string nameMessage, string passwordMessage)
    {
        if (NameGuideText != null)
            NameGuideText.text = nameMessage;

        if (PasswordGuideText != null)
            PasswordGuideText.text = passwordMessage;
    }

    private void ConfigureNetworkTimeouts()
    {
        if (nm == null)
            return;

        nm.NetworkConfig.ClientConnectionBufferTimeout = Mathf.Max(nm.NetworkConfig.ClientConnectionBufferTimeout, 60);
        nm.NetworkConfig.LoadSceneTimeOut = Mathf.Max(nm.NetworkConfig.LoadSceneTimeOut, 180);
    }

    private void DisableAutomaticPlayerPrefabSpawn()
    {
        if (nm == null)
            return;

        if (nm.NetworkConfig.AutoSpawnPlayerPrefabClientSide)
        {
            Debug.Log("[RelayBootstrap] PlayerSpawnService를 사용하므로 AutoSpawnPlayerPrefabClientSide를 비활성화합니다.");
            nm.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        }

        if (nm.NetworkConfig.PlayerPrefab != null)
        {
            Debug.Log("[RelayBootstrap] PlayerSpawnService를 사용하므로 NetworkManager 기본 PlayerPrefab 자동 스폰을 비활성화합니다.");
            nm.NetworkConfig.PlayerPrefab = null;
        }
    }

    private void SetupApproval()
    {
        if (nm == null)
            return;

        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = StaticApproval;
        Debug.Log($"[Relay] Connection approval configured on NetworkManager instance={nm.GetInstanceID()}");
    }

    private void PrepareNetworkManagerForStart()
    {
        if (NetworkManager.Singleton != null)
            nm = NetworkManager.Singleton;

        if (!nm)
            return;

        if (!utp || utp.gameObject != nm.gameObject)
            utp = nm.GetComponent<UnityTransport>();

        SetupApproval();
        DisableAutomaticPlayerPrefabSpawn();
        ConfigureNetworkTimeouts();
    }

    private static void StaticApproval(
        NetworkManager.ConnectionApprovalRequest req,
        NetworkManager.ConnectionApprovalResponse res)
    {
        res.Approved = true;
        res.CreatePlayerObject = false;
        res.Pending = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Net] ClientConnected: {clientId} server={nm != null && nm.IsServer} client={nm != null && nm.IsClient}");

        if (nm != null && nm.IsClient && !nm.IsServer && clientId == nm.LocalClientId)
        {
            localClientConnected = true;
            localClientDisconnected = false;
            lastDisconnectReason = string.Empty;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        string disconnectReason = nm != null ? nm.DisconnectReason : string.Empty;
        Debug.LogWarning($"[Net] ClientDisconnected: {clientId} server={nm != null && nm.IsServer} client={nm != null && nm.IsClient} reason={disconnectReason}");

        if (nm != null && nm.IsClient && !nm.IsServer)
        {
            localClientDisconnected = true;
            localClientConnected = false;
            lastDisconnectReason = disconnectReason;
        }
    }

    private void ResetClientConnectState()
    {
        waitingForLocalClientConnection = false;
        localClientConnected = false;
        localClientDisconnected = false;
        lastDisconnectReason = string.Empty;
    }

    private void LoadClientLoadingSceneIfNeeded()
    {
        if (SceneManager.GetActiveScene().name == "LoadingScene")
            return;

        SceneManager.LoadScene("LoadingScene");
    }

    private void ShutdownAndReturnClientToTitle()
    {
        if (nm != null && nm.IsListening)
            nm.Shutdown();

        if (SceneManager.GetActiveScene().name == "LoadingScene")
            SceneManager.LoadScene("TitleScene");
    }

    private void FailJoin(string msg)
    {
        if (ToastService.I != null)
            ToastService.I.Show(msg);

        Debug.LogError($"[Net] {msg}");
    }
}
