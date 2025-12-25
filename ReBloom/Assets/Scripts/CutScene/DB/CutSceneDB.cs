using BansheeGz.BGDatabase;
using System.Collections.Generic;

public class CutSceneDB
{
    private readonly Dictionary<int, CutSceneData> _cutScenes = new();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("CutScene_String");

        foreach (var e in meta.EntitiesToList())
        {
            var d = new CutSceneData
            {
                CutSceneID     = e.Get<int>("CutSceneID"),
                ImageName      = e.Get<string>("CutSceneImageName"),
                NextCutSceneID = e.Get<int>("NextCutSceneID"),
                TextKR         = e.Get<string>("CutSceneTextKR")
            };

            _cutScenes[d.CutSceneID] = d;
        }
    }

    public bool TryGet(int cutSceneId, out CutSceneData data)
        => _cutScenes.TryGetValue(cutSceneId, out data);

    public IReadOnlyDictionary<int, CutSceneData> GetAll()
        => _cutScenes;
}

