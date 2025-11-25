using System.Collections.Generic;
using BansheeGz.BGDatabase;
using UnityEngine;

public class ArcDB
{
    private Dictionary<int, ArcData> _arcs = new();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Building");
        foreach (var e in meta.EntitiesToList())
        {
            var d = new ArcData
            {
                arcId = e.Get<int>("ArcID"),
                name = e.Get<string>("ArcName"),
                tier = e.Get<int>("Tier"),
                arcType = e.Get<int>("ArcType"),
                energyInc = e.Get<float>("EnergyInc"),
                energyDec = e.Get<float>("EnergyDec"),
                researchInc = e.Get<float>("ResearchProgressInc"),
                greeningInc = e.Get<float>("GreeningInc"),
                unlockValue = e.Get<int>("UnlockValue"),
                installLimit = e.Get<int>("Installationlimit"),
                buildTime = e.Get<float>("ArcTime"),
                text = e.Get<string>("Text"),
                interactType = e.Get<int>("ArcInteraction"),
                interactTime = e.Get<float>("InteractionTime"),
                buildPrefab = e.Get<GameObject>("buildPrefab"),
                previewPrefab = e.Get<GameObject>("previewPrefab"),
            };
            _arcs[d.arcId] = d;
        }
    }

    public bool TryGet(int arcId, out ArcData data) => _arcs.TryGetValue(arcId, out data);

    public Dictionary<int, ArcData> GetAll() => _arcs;
}
