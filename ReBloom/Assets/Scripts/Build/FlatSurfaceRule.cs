using UnityEngine;

public class FlatSurfaceRule : IBuildRule
{
    private LayerMask buildableLayer;
    private float maxHeightDiff;
    private float maxSlopeDot; // Vector3.Dot(normal, Vector3.up) 최소값

    public FlatSurfaceRule(LayerMask buildableLayer, float maxHeightDiff, float maxSlopeAngleDeg)
    {
        this.buildableLayer = buildableLayer;
        this.maxHeightDiff = maxHeightDiff;
        this.maxSlopeDot = Mathf.Cos(maxSlopeAngleDeg * Mathf.Deg2Rad);
    }

    public bool Validate(ArcContext ctx, out string errorCode)
    {
        var fp = ctx.FootPrint;
        // 빌딩 기준 로컬 좌표에서 네 모서리 점 계산
        Vector3[] localPoints =
        {
            new Vector3(-fp.sizeX/2f, 0, -fp.sizeZ/2f),
            new Vector3(-fp.sizeX/2f, 0, fp.sizeZ/2f),
            new Vector3(fp.sizeX/2f, 0, -fp.sizeZ/2f),
            new Vector3(fp.sizeX/2f, 0, fp.sizeZ/2f),
            Vector3.zero // 중앙도 한 번 체크하고 싶으면
        };

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < localPoints.Length; i++)
        {
            // 로컬 -> 월드 변환
            Vector3 worldPoint = ctx.Position + ctx.Rotation * localPoints[i] + Vector3.up * 2f;
            // 위쪽에서 아래로 쏘기 (2f는 여유 높이)

            if (!Physics.Raycast(worldPoint, Vector3.down, out RaycastHit hit, 5f, buildableLayer))
            {
                errorCode = "NO_FLOOR";
                return false;
            }

            // 바닥 태그 검사(필요하다면)
            // if (!hit.collider.CompareTag("Floor")) { ... }

            // 평평 / 경사도 검사
            if (Vector3.Dot(hit.normal, Vector3.up) < maxSlopeDot)
            {
                errorCode = "SLOPE_TOO_STEEP";
                return false;
            }

            minY = Mathf.Min(minY, hit.point.y);
            maxY = Mathf.Max(maxY, hit.point.y);
        }

        if (maxY - minY > maxHeightDiff)
        {
            errorCode = "NOT_FLAT";
            return false;
        }

        errorCode = null;
        return true;
    }
}
