using System.Collections.Generic;
using UnityEngine;

public class OccupancyRule : IBuildRule
{
    public bool Validate(ArcContext ctx, out string errorCode)
    {
        errorCode = null;

        var cells = FootprintToCells(ctx);

        if (!GridOccupancyManager.I.CanOccupy(cells, ctx.IgnoreOccupancyInstance))
        {
            errorCode = "CELL_OCCUPIED";
            return false;
        }

        return true;
    }

    private IEnumerable<Vector2Int> FootprintToCells(ArcContext ctx)
    {
        int sx = Mathf.Max(1, Mathf.CeilToInt(ctx.FootPrint.sizeX / CorridorGrid.CellSize));
        int sz = Mathf.Max(1, Mathf.CeilToInt(ctx.FootPrint.sizeZ / CorridorGrid.CellSize));

        int rotIndex = CorridorGrid.GetRotIndex(ctx.Rotation);
        if (rotIndex % 2 == 1) (sx, sz) = (sz, sx);

        Vector2Int baseCell = CorridorGrid.WorldToCell(ctx.Position);

        int hx = sx / 2;
        int hz = sz / 2;

        for (int x = -hx; x < -hx + sx; x++)
            for (int z = -hz; z < -hz + sz; z++)
                yield return new Vector2Int(baseCell.x + x, baseCell.y + z);
    }
}
