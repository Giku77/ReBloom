using UnityEngine;
using UnityEngine.InputSystem;

public class BuildPlacementController : MonoBehaviour
{
    public static BuildPlacementController I;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float placeDistance = 3f;
    [SerializeField] private LayerMask groundMask;

    private ArcData currentArc;
    private GameObject previewInstance;
    private BuildPreviewVisual previewVisual;
    private bool isPlacing = false;

    private Vector3 lastValidPos;
    private Quaternion lastRot;
    private bool lastCanBuild;
    private string lastError;

    private void Awake()
    {
        I = this;
    }

    private void SetupPreview(GameObject preview)
    {
        int previewLayer = LayerMask.NameToLayer("BuildingPreview");
        foreach (var tr in preview.GetComponentsInChildren<Transform>(true))
            tr.gameObject.layer = previewLayer;
    }

    public void StartPlacement(ArcData arc, ArcRecipe arcRecipe, GameObject previewPrefab)
    {

        if (!BuildManager.I.HasMaterials(arcRecipe) && !BuildManager.I.debugMode)
        {
            ToastMessageUI.Instance.Show("재료가 부족합니다.");
            return;
        }

        CancelPlacement();

        currentArc = arc;

        var prefab = arc.previewPrefab != null ? arc.previewPrefab : previewPrefab;

        previewInstance = Instantiate(prefab);
        SetupPreview(previewInstance);
        previewVisual = previewInstance.GetComponent<BuildPreviewVisual>();

        isPlacing = true;
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        currentArc = null;

        if (previewInstance != null)
            Destroy(previewInstance);

        previewInstance = null;
        previewVisual = null;
    }

    private void Update()
    {
        if (!isPlacing || currentArc == null || previewInstance == null)
            return;

        // 1) 프리뷰 위치 / 회전 구하기
        Vector3 targetPos;
        Quaternion targetRot;

        GetTargetTransform(out targetPos, out targetRot);

        previewInstance.transform.position = targetPos;
        previewInstance.transform.rotation = targetRot;

        // 2) 설치 가능 여부 체크
        lastCanBuild = BuildManager.I.CanBuildAt(currentArc, targetPos, targetRot, out lastError);
        previewVisual?.SetValid(lastCanBuild);

        // 3) 입력 처리
        HandleInput(targetPos, targetRot);
    }

    private void GetTargetTransform(out Vector3 pos, out Quaternion rot)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out var hit, placeDistance, groundMask))
        {
            bool isCorridorBuild =
                currentArc != null &&
                currentArc.buildPrefab != null &&
                currentArc.buildPrefab.TryGetComponent<CorridorNode>(out var newNodePrefab);

            // 맞은 오브젝트에 기존 통로가 있는지
            var hitNode = hit.collider.GetComponentInParent<CorridorNode>();

            // ============ 1) 기존 통로에 "붙이는" 모드 ============ 
            if (isCorridorBuild && hitNode != null)
            {
                // 기준이 되는 셀
                Vector2Int baseCell = hitNode.Cell;

                // 기준 통로의 회전 (90도 단위라고 가정)
                float baseYaw = hitNode.transform.eulerAngles.y;
                int baseRotIndex = Mathf.RoundToInt(baseYaw / 90f) % 4;
                if (baseRotIndex < 0) baseRotIndex += 4;

                // 어느 면을 맞췄는지 (통로 로컬좌표 기준)
                Vector3 localHit = hitNode.transform.InverseTransformPoint(hit.point);
                CorridorDirection localDir;
                if (Mathf.Abs(localHit.x) > Mathf.Abs(localHit.z))
                    localDir = localHit.x > 0 ? CorridorDirection.East : CorridorDirection.West;
                else
                    localDir = localHit.z > 0 ? CorridorDirection.North : CorridorDirection.South;

                // 로컬 방향 → 월드 기준 방향
                CorridorDirection worldDir =
                    CorridorConnectionManager.RotateDirection(localDir, baseRotIndex);

                // worldDir 쪽 이웃 셀
                Vector2Int offset =
                    CorridorConnectionManager.DirectionToOffset(worldDir);
                Vector2Int targetCell = baseCell + offset;

                // 셀 센터로 위치 스냅
                float y = hitNode.transform.position.y;
                pos = CorridorGrid.CellToWorldCenter(targetCell, y);

                // 회전은 기준 통로와 동일하게 (일자/십자 통로는 이렇게만 해도 연결됨)
                rot = hitNode.transform.rotation;

                lastValidPos = pos;
                lastRot      = rot;
                return;
            }

            // ============ 2) 통로가 아니거나, 통로를 안 맞췄을 때 ============ 
            pos = hit.point;
            rot = Quaternion.LookRotation(playerTransform.forward, Vector3.up);

            // 통로를 짓는 중이면 그냥 그리드 스냅만 적용
            if (isCorridorBuild)
            {
                float yaw = playerTransform.eulerAngles.y;
                float snappedYaw = Mathf.Round(yaw / 90f) * 90f;
                rot = Quaternion.Euler(0, snappedYaw, 0);

                pos = CorridorGrid.Snap(pos);
            }
        }
        else
        {
            // 레이 아무것도 못 맞추면 그냥 플레이어 앞에 놓기
            pos = playerTransform.position + playerTransform.forward * placeDistance;
            rot = Quaternion.LookRotation(playerTransform.forward, Vector3.up);

            // 통로면 여기서도 옵션으로 스냅 걸어줄 수 있음
            if (currentArc != null &&
                currentArc.buildPrefab != null &&
                currentArc.buildPrefab.TryGetComponent<CorridorNode>(out _))
            {
                float yaw = playerTransform.eulerAngles.y;
                float snappedYaw = Mathf.Round(yaw / 90f) * 90f;
                rot = Quaternion.Euler(0, snappedYaw, 0);

                pos = CorridorGrid.Snap(pos);
            }
        }

        lastValidPos = pos;
        lastRot      = rot;
    }

    // private void GetTargetTransform(out Vector3 pos, out Quaternion rot)
    // {
    //     // 간단 버전: 플레이어 전방 Raycast → 히트 포인트에 배치
    //     Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
    //     if (Physics.Raycast(ray, out var hit, placeDistance, groundMask))
    //     {
    //         pos = hit.point;
    //     }
    //     else
    //     {
    //         // 못 맞추면 그냥 플레이어 앞에 고정 거리
    //         pos = playerTransform.position + playerTransform.forward * placeDistance;
    //     }

    //     //pos += Vector3.up * 1.1f; // 약간 띄우기
    //     rot = Quaternion.LookRotation(playerTransform.forward, Vector3.up);

    //     if (currentArc != null &&
    //     currentArc.buildPrefab != null &&
    //     currentArc.buildPrefab.TryGetComponent<CorridorNode>(out _))
    //     {
    //         pos = CorridorGrid.Snap(pos);
    //     }
    //     lastValidPos = pos;
    //     lastRot = rot;
    // }

    private void HandleInput(Vector3 pos, Quaternion rot)
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        if (mouse == null) return; 

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (lastCanBuild)
            {
                Vector3 spawnPos = pos;

                if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out var hit, 20f, groundMask))
                {
                    spawnPos = hit.point;
                }
                bool built = BuildManager.I.TryBuild(currentArc.arcId, spawnPos, rot);
                if (built)
                    CancelPlacement();
            }
            else
            {
                ToastMessageUI.Instance?.Show($"설치 불가: {lastError}");
            }
        }

        if (mouse.rightButton.wasPressedThisFrame ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
        }
    }
}