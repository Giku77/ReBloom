using UnityEngine;

public class CorridorAttachRule : IBuildRule
{
    public bool Validate(ArcContext ctx, out string errorCode)
    {
        errorCode = null;

        // 통로가 아니면 이 룰은 패스
        if (ctx.ArcPrefab == null || !ctx.ArcPrefab.TryGetComponent<CorridorNode>(out var nodePrefab))
            return true;

        // 후보 위치의 셀
        var cell = CorridorGrid.WorldToCell(ctx.Position);

        // 현재 회전
        float yRot = ctx.Rotation.eulerAngles.y;
        int rotIndex = Mathf.RoundToInt(yRot / 90f) % 4;
        if (rotIndex < 0) rotIndex += 4;

        // 네 방향 체크
        foreach (var localDir in new[] { CorridorDirection.North, CorridorDirection.East, CorridorDirection.South, CorridorDirection.West })
        {
            // 우리 통로가 이 방향으로 열려있는지 (로컬 기준)
            bool hasOpeningLocal = nodePrefab.HasOpening(localDir);
            var worldDir = CorridorConnectionManager.RotateDirection(localDir, rotIndex);
            var offset   = CorridorConnectionManager.DirectionToOffset(worldDir);
            var neighborCell = cell + offset;

            // 이 방향에 이웃 통로가 없으면 아무 제약 없음
            if (!CorridorConnectionManager.I.TryGetNodeAt(neighborCell, out var neighbor))
                continue;

            // 이웃이 우리 쪽을 향해 열려있는지
            var oppositeWorld = CorridorNode.Opposite(worldDir);
            var neighborLocalToUs = CorridorConnectionManager.RotateDirectionBack(oppositeWorld, neighbor.transform);
            bool neighborHasOpening = neighbor.HasOpening(neighborLocalToUs);

            // ***여기가 핵심 조건***
            // 1) 이 방향에 이미 이웃 통로가 있다
            // 2) 둘 중 하나라도 opening이 없다
            // => 이 방향으로는 "막힌 벽끼리 맞닥뜨리는" 상황이니 설치 금지
            if (!(hasOpeningLocal && neighborHasOpening))
            {
                errorCode = "CORRIDOR_BLOCKED_SIDE";
                return false;
            }

            // 2) 이웃이 통로가 아니면 "소켓"을 본다
            // 이웃 셀에 소켓이 있고, 그 소켓이 우리를 향해 열려있다면 연결 가능
            var needSocketDir = CorridorNode.Opposite(worldDir); // 이웃(온실) 입장에선 우리 쪽 방향
            bool neighborHasSocket = CorridorSocketManager.I != null &&
                                     CorridorSocketManager.I.HasSocket(neighborCell, needSocketDir);

            // 소켓이 존재하는데, 우리 쪽 opening이 없으면 막힌 면이라서 금지
            if (neighborHasSocket && !hasOpeningLocal)
            {
                errorCode = "CORRIDOR_BLOCKED_SIDE";
                return false;
            }
        }

        return true;
    }
}
