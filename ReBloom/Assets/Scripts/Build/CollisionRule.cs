using UnityEngine;

public class CollisionRule : IBuildRule
{
    private LayerMask obstacleLayers;

    // 빌딩 데이터에 따로 bounds 정보를 저장해두거나,
    // prefab에서 가져와서 캐시해도 됨.
    public CollisionRule(LayerMask obstacleLayers)
    {
        this.obstacleLayers = obstacleLayers;
    }

    public bool Validate(ArcContext ctx, out string errorCode)
    {
        var fp = ctx.FootPrint;
        Vector3 halfExtents = new Vector3(fp.sizeX / 2f, 2f, fp.sizeZ / 2f); // 높이는 대충 2~3m 여유

        // 회전 고려해서 OverlapBox
        Collider[] hits = Physics.OverlapBox(
            ctx.Position + Vector3.up * halfExtents.y,
            halfExtents,
            ctx.Rotation,
            obstacleLayers
        );

        if (hits.Length > 0)
        {
            // 여기서 태그로 자기 프리뷰/무시 대상 제외 로직 넣어도 됨
            errorCode = "COLLISION";
            return false;
        }

        errorCode = null;
        return true;
    }
}
