using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CollisionRule : IBuildRule
{
    private LayerMask obstacleLayers;

    // ���� �����Ϳ� ���� bounds ������ �����صΰų�,
    // prefab���� �����ͼ� ĳ���ص� ��.
    public CollisionRule(LayerMask obstacleLayers)
    {
        this.obstacleLayers = obstacleLayers;
    }

    public bool Validate(ArcContext ctx, out BuildError errorCode)
    {
        var fp = ctx.FootPrint;
        // const float margin = 0.02f;
        // Vector3 halfExtents = new Vector3(
        //     fp.sizeX / 2f - margin,
        //     2f,
        //     fp.sizeZ / 2f - margin
        // );
        Vector3 halfExtents = new Vector3(fp.sizeX / 2f, 2f, fp.sizeZ / 2f); // ���̴� ���� 2~3m ����

        DrawWireBox(ctx.Position + Vector3.up * halfExtents.y, halfExtents, ctx.Rotation, Color.red, 1f);

        // ȸ�� �����ؼ� OverlapBox
        Collider[] hits = Physics.OverlapBox(
            ctx.Position + Vector3.up * halfExtents.y,
            halfExtents,
            ctx.Rotation,
            obstacleLayers
        );

        bool isCorridor = ctx.ArcPrefab != null &&
                    ctx.ArcPrefab.GetComponent<CorridorNode>() != null;
        Vector2Int candidateCell = default;
        if (isCorridor)
        {
            candidateCell = CorridorGrid.WorldToCell(ctx.Position);
        }

        foreach (var col in hits)
        {
            // �ڱ� �����䳪 Ư�� �±�/���̾�� ���⼭�� �ɷ��� �� ����
            // if (col.CompareTag("BuildPreview")) continue;
            if (isCorridor)
            {
                var otherNode = col.GetComponentInParent<CorridorNode>();
                if (otherNode != null)
                {
                    // ���� ���� �̹� ��ΰ� ������ ����
                    if (otherNode.Cell == candidateCell)
                    {
                        errorCode = BuildError.Colllision;
                        return false;
                    }

                    continue;
                }
            }

            errorCode = BuildError.Colllision;
            return false;
        }


        // if (hits.Length > 0)
        // {
        //     // ���⼭ �±׷� �ڱ� ������/���� ��� ���� ���� �־ ��
        //     errorCode = "COLLISION";
        //     return false;
        // }

        errorCode = BuildError.None;
        return true;
    }

    private void DrawWireBox(Vector3 center, Vector3 halfExtents, Quaternion rot, Color color, float duration)
    {
        Vector3[] corners = new Vector3[8];
        int i = 0;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    corners[i++] = center + rot * Vector3.Scale(halfExtents, new Vector3(x, y, z));

        int[,] edges = {
        {0,1},{0,2},{0,4},{7,6},{7,5},{7,3},{1,3},{1,5},{2,3},{2,6},{4,5},{4,6}
    };
        for (int e = 0; e < edges.GetLength(0); e++)
        {
            Debug.DrawLine(corners[edges[e, 0]], corners[edges[e, 1]], color, duration);
        }
    }
}
