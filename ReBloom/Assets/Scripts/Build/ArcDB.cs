using System.Collections.Generic;
using BansheeGz.BGDatabase;

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
            };
            _arcs[d.arcId] = d;
        }
    }

    public bool TryGet(int arcId, out ArcData data) => _arcs.TryGetValue(arcId, out data);
}
