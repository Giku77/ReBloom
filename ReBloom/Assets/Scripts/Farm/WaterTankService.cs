using System.Collections.Generic;
using UnityEngine;

public class WaterTankService : MonoBehaviour
{
    public static WaterTankService I { get; private set; }

    [SerializeField] private GameInventory inventory;

    private readonly HashSet<WaterTankInteractable> registeredTanks = new();

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        RegisterExistingTanks();
    }

    private void OnDestroy()
    {
        if (I == this)
            I = null;
    }

    public void RegisterTank(WaterTankInteractable tank)
    {
        if (tank == null)
            return;

        registeredTanks.Add(tank);
    }

    public void UnregisterTank(WaterTankInteractable tank)
    {
        if (tank == null)
            return;

        registeredTanks.Remove(tank);
    }

    public WaterTankInteractable GetPrimaryTank()
    {
        foreach (var tank in registeredTanks)
        {
            if (tank != null && tank.isActiveAndEnabled)
                return tank;
        }

        RegisterExistingTanks();

        foreach (var tank in registeredTanks)
        {
            if (tank != null && tank.isActiveAndEnabled)
                return tank;
        }

        return null;
    }

    public WaterTankInteractable FindClosestTank(Vector3 position)
    {
        WaterTankInteractable closest = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (var tank in registeredTanks)
        {
            if (tank == null || !tank.isActiveAndEnabled)
                continue;

            float distanceSqr = (tank.transform.position - position).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr)
                continue;

            closest = tank;
            closestDistanceSqr = distanceSqr;
        }

        if (closest != null)
            return closest;

        RegisterExistingTanks();
        return GetPrimaryTank();
    }

    private void RegisterExistingTanks()
    {
        var tanks = FindObjectsByType<WaterTankInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var tank in tanks)
            RegisterTank(tank);
    }
}
