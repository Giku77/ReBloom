using UnityEngine;

public class CorridorCellRule : IBuildRule
{
    public bool Validate(ArcContext ctx, out BuildError errorCode)
    {
        errorCode = BuildError.None;

        // 통로가 아니면 이 룰은 패스
        if (ctx.ArcPrefab == null ||
            !ctx.ArcPrefab.TryGetComponent<CorridorNode>(out _))
        {
            return true;
        }

        // 이 통로가 놓일 셀
        Vector2Int cell = CorridorGrid.WorldToCell(ctx.Position);

        // 이미 그 셀에 등록된 통로가 있으면 설치 불가
        if (CorridorConnectionManager.I.TryGetNodeAt(cell, out var existing) && existing != null)
        {
            errorCode = BuildError.CellOccupied;  
            return false;
        }

        return true;
    }
}
