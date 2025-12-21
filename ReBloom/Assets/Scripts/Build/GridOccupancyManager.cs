using System.Collections.Generic;
using UnityEngine;

public class GridOccupancyManager : MonoBehaviour
{
    public static GridOccupancyManager I;

    private readonly Dictionary<Vector2Int, BuildingInstance> occupied = new();
    private readonly Dictionary<BuildingInstance, HashSet<Vector2Int>> cellsByInst = new();

    void Awake() => I = this;

    public bool CanOccupy(IEnumerable<Vector2Int> cells, BuildingInstance ignore = null)
    {
        foreach (var c in cells)
        {
            if (occupied.TryGetValue(c, out var owner) && owner != null && owner != ignore)
                return false;
        }
        return true;
    }

    public void Occupy(BuildingInstance inst, IEnumerable<Vector2Int> cells)
    {
        if (inst == null) return;

        // 기존 점유 해제 후 새로 등록하는 방식(안전)
        Release(inst);

        if (!cellsByInst.TryGetValue(inst, out var set))
        {
            set = new HashSet<Vector2Int>();
            cellsByInst[inst] = set;
        }

        foreach (var c in cells)
        {
            occupied[c] = inst;
            set.Add(c);
        }
    }

    public void Release(BuildingInstance inst)
    {
        if (inst == null) return;

        if (!cellsByInst.TryGetValue(inst, out var set))
            return;

        foreach (var c in set)
        {
            if (occupied.TryGetValue(c, out var owner) && owner == inst)
                occupied.Remove(c);
        }

        set.Clear();
        cellsByInst.Remove(inst);
    }
}
