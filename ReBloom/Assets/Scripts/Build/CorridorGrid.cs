using UnityEngine;

public static class CorridorGrid
{
    public const float CellSize = 3.91f;

    public static Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / CellSize);
        int z = Mathf.RoundToInt(worldPos.z / CellSize);
        return new Vector2Int(x, z);
    }

    public static Vector3 CellToWorldCenter(Vector2Int cell, float y)
    {
        return new Vector3(cell.x * CellSize, y, cell.y * CellSize);
    }

    public static Vector3 Snap(Vector3 worldPos)
    {
        var cell = WorldToCell(worldPos);
        return CellToWorldCenter(cell, worldPos.y);
    }

    public static Vector2Int RotateOffset(Vector2Int offset, int rotIndex)
    {
        Vector2Int result = offset;
        for (int i = 0; i < rotIndex; i++)
        {
            // (x,z) -> (z, -x)  회전
            result = new Vector2Int(result.y, -result.x);
        }
        return result;
    }

    public static int GetRotIndex(Quaternion rot)
    {
        float y = rot.eulerAngles.y;
        int idx = Mathf.RoundToInt(y / 90f) % 4;
        if (idx < 0) idx += 4;
        return idx;
    }
}
