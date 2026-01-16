using UnityEngine;

public class FlatSurfaceRule : IBuildRule
{
    private LayerMask buildableLayer;
    private float maxHeightDiff;
    private float maxSlopeDot; // Vector3.Dot(normal, Vector3.up) �ּҰ�

    public FlatSurfaceRule(LayerMask buildableLayer, float maxHeightDiff, float maxSlopeAngleDeg)
    {
        this.buildableLayer = buildableLayer;
        this.maxHeightDiff = maxHeightDiff;
        this.maxSlopeDot = Mathf.Cos(maxSlopeAngleDeg * Mathf.Deg2Rad);
    }

    public bool Validate(ArcContext ctx, out BuildError errorCode)
    {
        var fp = ctx.FootPrint;
        // ���� ���� ���� ��ǥ���� �� �𼭸� �� ���
        Vector3[] localPoints =
        {
            new Vector3(-fp.sizeX/2f, 0, -fp.sizeZ/2f),
            new Vector3(-fp.sizeX/2f, 0, fp.sizeZ/2f),
            new Vector3(fp.sizeX/2f, 0, -fp.sizeZ/2f),
            new Vector3(fp.sizeX/2f, 0, fp.sizeZ/2f),
            Vector3.zero // �߾ӵ� �� �� üũ�ϰ� ������
        };

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < localPoints.Length; i++)
        {
            // ���� -> ���� ��ȯ
            Vector3 worldPoint = ctx.Position + ctx.Rotation * localPoints[i] + Vector3.up * 2f;
            // ���ʿ��� �Ʒ��� ��� (2f�� ���� ����)

            if (!Physics.Raycast(worldPoint, Vector3.down, out RaycastHit hit, 5f, buildableLayer))
            {
                errorCode = BuildError.NoGround;
                return false;
            }

            // �ٴ� �±� �˻�(�ʿ��ϴٸ�)
            // if (!hit.collider.CompareTag("Floor")) { ... }

            // ���� / ��絵 �˻�
            if (Vector3.Dot(hit.normal, Vector3.up) < maxSlopeDot)
            {
                errorCode = BuildError.SlopeTooHigh;
                return false;
            }

            minY = Mathf.Min(minY, hit.point.y);
            maxY = Mathf.Max(maxY, hit.point.y);
        }

        //Debug.Log($"M : {maxY - minY}");

        if (maxY - minY > maxHeightDiff)
        {
            errorCode = BuildError.NotFlat;
            return false;
        }

        errorCode = BuildError.None;
        return true;
    }
}
