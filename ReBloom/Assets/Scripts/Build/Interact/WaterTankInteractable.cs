using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BuildingInstance))]
[RequireComponent(typeof(NetworkObject))]
public class WaterTankInteractable : NetworkBehaviour, IInteractable
{
    private const int StageID = 400;
    private const int MinWaterLevel = 0;
    private const int MaxWaterLevel = 100;
    private const int ManualStep = 10;
    private const int RainStep = 5;
    private const float RainInterval = 10f;
    private const int FilledWaterItemId = 4002002;
    private const int EmptyBottleItemId = 4102035;
    private const int AirPurifierArcId = 3103007;

    [SerializeField] private BuildingInstance building;

    private readonly NetworkVariable<int> waterLevel = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool isRaining;
    private bool hasAirPurifier;
    private float rainTimer;
    private int localWaterLevel;

    public event Action<int> OnWaterLevelChanged;

    public float HoldTime => 0f;
    public int WaterLevel => IsNetworkSession ? waterLevel.Value : localWaterLevel;

    private bool IsNetworkSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    private bool HasServerAuthority => !IsNetworkSession || (IsSpawned && IsServer);

    private void Awake()
    {
        if (building == null)
            building = GetComponent<BuildingInstance>();
    }

    private void OnEnable()
    {
        StageManager.OnWeatherChange += OnWeatherChanged;
        RefreshWeatherState();
        WaterTankService.I?.RegisterTank(this);
    }

    private void OnDisable()
    {
        StageManager.OnWeatherChange -= OnWeatherChanged;
        WaterTankService.I?.UnregisterTank(this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        waterLevel.OnValueChanged += HandleWaterLevelChanged;
        HandleWaterLevelChanged(waterLevel.Value, waterLevel.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        waterLevel.OnValueChanged -= HandleWaterLevelChanged;
    }

    private void Update()
    {
        if (!HasServerAuthority)
            return;

        Tick(Time.deltaTime);
    }

    public bool CanInteract()
    {
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (player == null)
            return;

        player.OpenWaterTankUI(this);
    }

    public void RequestStoreWaterFromLocalPlayer()
    {
        if (IsNetworkSession)
        {
            if (IsServer)
                TryStoreWaterServer(NetworkManager.ServerClientId);
            else
                RequestStoreWaterRpc();
            return;
        }

        var player = FindLocalPlayer();
        if (player?.Inventory == null)
            return;

        TryStoreWaterLocal(player.Inventory);
    }

    public void RequestRetrieveWaterFromLocalPlayer()
    {
        if (IsNetworkSession)
        {
            if (IsServer)
                TryRetrieveWaterServer(NetworkManager.ServerClientId);
            else
                RequestRetrieveWaterRpc();
            return;
        }

        var player = FindLocalPlayer();
        if (player?.Inventory == null)
            return;

        TryRetrieveWaterLocal(player.Inventory);
    }

    public bool TryConsumeWater(int amount)
    {
        if (amount <= 0)
            return true;

        if (IsNetworkSession && !IsServer)
            return false;

        return TryConsumeWaterInternal(amount);
    }

    private void Tick(float deltaTime)
    {
        if (BuildManager.I != null)
            hasAirPurifier = BuildManager.I.GetCount(AirPurifierArcId) > 0;

        if (!isRaining || !hasAirPurifier)
            return;

        rainTimer += deltaTime;
        if (rainTimer < RainInterval)
            return;

        rainTimer -= RainInterval;
        AddWaterDelta(RainStep);
    }

    private void OnWeatherChanged(int stageID, WeatherType weather)
    {
        if (stageID != StageID)
            return;

        isRaining = weather == WeatherType.Rain || weather == WeatherType.Thunder;
        if (!isRaining)
            rainTimer = 0f;
    }

    private void RefreshWeatherState()
    {
        var stageManager = FindFirstObjectByType<StageManager>();
        if (stageManager == null)
            return;

        var info = stageManager.GetWeatherInfo(StageID);
        if (info == null)
            return;

        isRaining = info.currentWeather == WeatherType.Rain || info.currentWeather == WeatherType.Thunder;
        if (!isRaining)
            rainTimer = 0f;
    }

    private void HandleWaterLevelChanged(int _, int current)
    {
        if (!IsNetworkSession)
            localWaterLevel = current;

        OnWaterLevelChanged?.Invoke(current);
    }

    private bool TryStoreWaterServer(ulong clientId)
    {
        if (!TryGetPlayerInventory(clientId, out var inventory))
            return false;

        return TryStoreWaterLocal(inventory);
    }

    private bool TryRetrieveWaterServer(ulong clientId)
    {
        if (!TryGetPlayerInventory(clientId, out var inventory))
            return false;

        return TryRetrieveWaterLocal(inventory);
    }

    private bool TryStoreWaterLocal(PlayerInventoryRuntime inventory)
    {
        if (inventory == null)
            return false;

        if (!inventory.HasItem(FilledWaterItemId, 1))
        {
            ToastMessageUI.Instance?.Show("물탱크에 담을 물이 없습니다.");
            return false;
        }

        if (WaterLevel >= MaxWaterLevel)
        {
            ToastMessageUI.Instance?.Show("물이 넘칠 듯 합니다.");
            return false;
        }

        if (!inventory.TryRemoveItem(FilledWaterItemId, 1))
            return false;

        if (inventory.AddItemFromWorld(EmptyBottleItemId, 1) <= 0)
        {
            inventory.AddItemFromWorld(FilledWaterItemId, 1);
            ToastMessageUI.Instance?.Show("인벤토리 공간이 부족합니다.");
            return false;
        }

        AddWaterDelta(ManualStep);
        return true;
    }

    private bool TryRetrieveWaterLocal(PlayerInventoryRuntime inventory)
    {
        if (inventory == null)
            return false;

        if (!inventory.HasItem(EmptyBottleItemId, 1))
        {
            ToastMessageUI.Instance?.Show("물을 담을 빈 통이 없습니다.");
            return false;
        }

        if (WaterLevel <= MinWaterLevel)
        {
            ToastMessageUI.Instance?.Show("물이 거의 없습니다.");
            return false;
        }

        if (!inventory.TryRemoveItem(EmptyBottleItemId, 1))
            return false;

        if (inventory.AddItemFromWorld(FilledWaterItemId, 1) <= 0)
        {
            inventory.AddItemFromWorld(EmptyBottleItemId, 1);
            ToastMessageUI.Instance?.Show("인벤토리 공간이 부족합니다.");
            return false;
        }

        AddWaterDelta(-ManualStep);
        Debug.Log("[WaterTankInteractable] 물 회수 성공");
        return true;
    }

    private bool TryConsumeWaterInternal(int amount)
    {
        if (WaterLevel < amount)
            return false;

        AddWaterDelta(-amount);
        return true;
    }

    private void AddWaterDelta(int delta)
    {
        int newValue = Mathf.Clamp(WaterLevel + delta, MinWaterLevel, MaxWaterLevel);
        if (newValue == WaterLevel)
            return;

        if (IsNetworkSession)
        {
            if (!IsServer)
                return;

            waterLevel.Value = newValue;
            return;
        }

        int previous = localWaterLevel;
        localWaterLevel = newValue;
        HandleWaterLevelChanged(previous, localWaterLevel);
    }

    private bool TryGetPlayerInventory(ulong clientId, out PlayerInventoryRuntime inventory)
    {
        inventory = null;

        if (NetworkManager.Singleton == null)
            return false;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            return false;

        inventory = client.PlayerObject.GetComponent<PlayerInventoryRuntime>();
        return inventory != null;
    }

    private PlayerController FindLocalPlayer()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            var localPlayer = nm.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
                return localPlayer.GetComponent<PlayerController>();
        }

        return FindFirstObjectByType<PlayerController>();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStoreWaterRpc(RpcParams rpcParams = default)
    {
        TryStoreWaterServer(rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestRetrieveWaterRpc(RpcParams rpcParams = default)
    {
        TryRetrieveWaterServer(rpcParams.Receive.SenderClientId);
    }
}
