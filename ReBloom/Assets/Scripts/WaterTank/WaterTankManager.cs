using System;
using UnityEngine;

public class WaterTankManager
{
    private readonly GameInventory inventoryItemData;

    private const int StageID = 400;

    private const int MinWaterLevel = 0;
    private const int MaxWaterLevel = 100;

    private const int ManualStep = 10;
    private const int RainStep = 5;
    private const float RainInterval = 10f;

    private int waterLevel = 0;

    public bool isRaining = false;
    private bool hasAirPurifier = false;
    private float rainTimer = 0f;

    private int airPurifierArcId = 3103007;

    public static event Action<int> OnWaterLevelChange;

    public int WaterLevel => waterLevel;

    public WaterTankManager(GameInventory inventory)
    {
        inventoryItemData = inventory;

        StageManager.OnWeatherChange += OnWeatherChanged;

        var stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
        if (stageManager != null)
        {
            var info = stageManager.GetWeatherInfo(StageID);
            if (info != null)
            {
                isRaining = info.currentWeather == WeatherType.Rain;
            }
        }
    }

    public void StoreWater()
    {
        if (inventoryItemData == null) return;

        if (!inventoryItemData.HasItem(4002002, 1))
        {
            ToastMessageUI.Instance.Show("물탱크에 담을 물이 없습니다.");
            return;
        }

        if (waterLevel >= MaxWaterLevel)
        {
            ToastMessageUI.Instance.Show("물이 넘칠 듯 합니다.");
            return;
        }

        inventoryItemData.RemoveItem(4002002, 1);
        inventoryItemData.AddItemFromWorld(4102035, 1);

        AddWater(ManualStep);
    }

    public void RetrieveWater()
    {
        if (inventoryItemData == null) return;

        if (!inventoryItemData.HasItem(4102035, 1))
        {
            ToastMessageUI.Instance.Show("물을 담을 빈 통이 없습니다.");
            return;
        }

        if (waterLevel <= MinWaterLevel)
        {
            ToastMessageUI.Instance.Show("물이 거의 없습니다.");
            return;
        }

        inventoryItemData.RemoveItem(4102035, 1);
        inventoryItemData.AddItemFromWorld(4002002, 1);

        AddWater(-ManualStep);
        Debug.Log("[WaterTankManager] 물 회수 성공");
    }

    public void Tick(float deltaTime)
    {
        if (BuildManager.I != null)
            hasAirPurifier = BuildManager.I.GetCount(airPurifierArcId) > 0;

        if (!isRaining || !hasAirPurifier)
            return;

        rainTimer += deltaTime;

        if (rainTimer >= RainInterval)
        {
            rainTimer -= RainInterval;
            AddWater(RainStep);
        }
    }

    private void OnWeatherChanged(int stageID, WeatherType weather)
    {
        if (stageID != StageID)
            return;

        isRaining = (weather == WeatherType.Rain) || (weather == WeatherType.Thunder);

        if (!isRaining)
            rainTimer = 0f;
    }

    public void SetAirPurifierInstalled(bool installed)
    {
        hasAirPurifier = installed;

        if (!installed)
            rainTimer = 0f;
    }

    private void AddWater(int delta)
    {
        int newValue = Mathf.Clamp(waterLevel + delta, MinWaterLevel, MaxWaterLevel);

        if (newValue == waterLevel) return;

        waterLevel = newValue;
        OnWaterLevelChange?.Invoke(waterLevel);
    }

    public void Dispose()
    {
        StageManager.OnWeatherChange -= OnWeatherChanged;
    }
}
