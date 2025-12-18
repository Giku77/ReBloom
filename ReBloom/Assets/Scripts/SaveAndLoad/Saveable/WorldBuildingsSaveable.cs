using System.Linq;
using UnityEngine;

public class WorldBuildingsSaveable : MonoBehaviour, ISaveable
{
    public string EntityGuid => "world_buildings";

    public void Capture(SaveGameDTO save)
    {
        var bm = BuildManager.I;
        if (bm == null) return;

        save.world.placedBuildings.Clear();

        foreach (var inst in bm.EnumerateAllInstances())
        {
            if (inst == null) continue;

            var id = inst.GetComponent<SaveableEntity>();
            if (id == null) continue;

            save.world.placedBuildings.Add(new BuildingInstanceSaveDTO
            {
                guid = id.PersistentId,
                prefabId = inst.ArcId,                      
                transform = TransformDTO.From(inst.transform),
                containerGuid = null                         // 창고 연결은 다음 단계에서 확장
            });
        }
        Debug.Log($"[Buildings Capture] found={bm.EnumerateAllInstances().Count()}, saved={save.world.placedBuildings.Count}");
    }

    public void Restore(SaveGameDTO save)
    {
        var bm = BuildManager.I;
        if (bm == null) return;

        bm.ClearAllBuildingsForLoad();

        foreach (var b in save.world.placedBuildings)
        {
            var pos = new Vector3(b.transform.px, b.transform.py, b.transform.pz);
            var rot = Quaternion.Euler(b.transform.rx, b.transform.ry, b.transform.rz);
            bm.SpawnForLoad(b.prefabId, pos, rot, b.guid);
        }
    }
}
