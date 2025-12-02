using UnityEngine;

/// <summary>
/// 아이템 월드 드롭 위치 계산
/// 인벤토리 로봇에 부착
/// </summary>
public class ItemDropCalculator : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 2f;
    [SerializeField] private float dropHeight = 1.5f;
    [SerializeField] private Vector3 dropOffset = Vector3.zero;

    [Header("Ground Detection")]
    [SerializeField] private bool useGroundDetection = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 10f;

    /// <summary>
    /// 드롭 위치 계산
    /// </summary>
    public Vector3 CalculateDropPosition()
    {
        // 전방 방향 계산
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        // 기본 드롭 위치
        Vector3 dropPosition = transform.position
            + forward * dropDistance
            + Vector3.up * dropHeight
            + dropOffset;

        // 지면 감지 사용 시
        if (useGroundDetection)
        {
            Vector3 groundPosition = FindGroundPosition(dropPosition);
            if (groundPosition != Vector3.zero)
            {
                dropPosition = groundPosition + Vector3.up * dropHeight;
            }
        }
        Debug.Log($"[ItemDropCalculator]{dropPosition}");
        return dropPosition;
    }

    /// <summary>
    /// 지면 위치 찾기
    /// </summary>
    private Vector3 FindGroundPosition(Vector3 startPosition)
    {
        Ray ray = new Ray(startPosition, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundRaycastDistance, groundLayer))
        {
            return hit.point;
        }

        return Vector3.zero;
    }
}