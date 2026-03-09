using Unity.Netcode;
using UnityEngine;

public class StatusPanelInteractable : NetworkBehaviour, IInteractable
{
    [SerializeField] protected BuildingInstance building;

    [Header("World UI Prefab (Canvas WorldSpace)")]
    [SerializeField] private StatusPanelUI worldPanelPrefab;

    [Header("Attach Point (defaults to self)")]
    [SerializeField] private Transform attachPoint;

    [Header("Offset / LookAt")]
    [SerializeField] private Vector3 localOffset = new(0f, 2.0f, 0f);
    [SerializeField] private bool faceCamera = true;

    private readonly NetworkVariable<bool> panelVisibleState =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private StatusPanelUI spawned;
    private Camera mainCam;

    public float HoldTime => 0.2f;

    protected virtual void Awake()
    {
        if (building == null)
            building = GetComponent<BuildingInstance>();

        if (attachPoint == null)
            attachPoint = transform;

        mainCam = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        panelVisibleState.OnValueChanged += HandlePanelVisibleChanged;
        ApplyPanelVisible(panelVisibleState.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        panelVisibleState.OnValueChanged -= HandlePanelVisibleChanged;
    }

    public void Interact(PlayerController player)
    {
        if (worldPanelPrefab == null)
        {
            Debug.LogWarning("[StatusPanelInteractable] worldPanelPrefab is missing");
            return;
        }

        bool next = !GetCurrentVisibleState();

        if (IsNetworkedSession())
        {
            PlayToggleSound(next);

            if (IsServer)
                SetPanelVisibleServer(next);
            else
                RequestTogglePanelRpc(next);

            return;
        }

        ApplyPanelVisible(next);
        PlayToggleSound(next);
    }

    private bool IsNetworkedSession()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned;
    }

    private bool GetCurrentVisibleState()
    {
        if (IsNetworkedSession())
            return panelVisibleState.Value;

        return spawned != null && spawned.gameObject.activeSelf;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestTogglePanelRpc(bool nextVisible)
    {
        SetPanelVisibleServer(nextVisible);
    }

    private void SetPanelVisibleServer(bool nextVisible)
    {
        if (!IsServer)
            return;

        panelVisibleState.Value = nextVisible;
        ApplyPanelVisible(nextVisible);
    }

    private void HandlePanelVisibleChanged(bool previous, bool current)
    {
        ApplyPanelVisible(current);
    }

    private void ApplyPanelVisible(bool visible)
    {
        if (visible)
        {
            EnsureSpawned();
            spawned.RefreshAll();
        }

        if (spawned != null)
            spawned.gameObject.SetActive(visible);
    }

    private void EnsureSpawned()
    {
        if (spawned != null)
            return;

        spawned = Instantiate(worldPanelPrefab, attachPoint);
        spawned.transform.localPosition = localOffset;
        spawned.transform.localRotation = Quaternion.identity;
        spawned.gameObject.SetActive(false);
    }

    private void PlayToggleSound(bool visible)
    {
        if (visible)
            SoundManager.I?.PlayTvOn();
        else
            SoundManager.I?.PlayTvOff();
    }

    private void LateUpdate()
    {
        if (!faceCamera || spawned == null || !spawned.gameObject.activeSelf)
            return;

        if (mainCam == null)
            mainCam = Camera.main;
        if (mainCam == null)
            return;

        Transform t = spawned.transform;
        Vector3 dir = t.position - mainCam.transform.position;
        t.rotation = Quaternion.LookRotation(dir);
    }

    public bool CanInteract()
    {
        return true;
    }
}
