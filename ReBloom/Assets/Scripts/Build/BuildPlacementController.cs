using LineworkLite.FreeOutline;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildPlacementController : MonoBehaviour
{
    public static BuildPlacementController I;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float placeDistance = 3f;
    [SerializeField] private float editPickDistance = 10f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask buildingMask;

    private ArcData currentArc;
    private GameObject previewInstance;
    private BuildPreviewVisual previewVisual;
    private OutlineToggle outlineToggle;
    private bool isPlacing = false;
    public bool IsPlacing => isPlacing;
    private Vector3 lastValidPos;
    private Quaternion lastRot;
    private bool lastCanBuild;
    private string lastError;

    private bool isEditMode = false;              // C로 토글되는 전체 “건축 편집 모드”
    private bool isMovingExisting = false;        // 기존 건물 이동 중인지
    private BuildingInstance hoveredBuilding;     // 카메라가 바라보고 있는 건물
    private BuildingInstance movingBuilding;      // 실제로 이동 중인 건물
    private Vector3 moveStartPos;
    private Quaternion moveStartRot;
    private string moveError;
    private bool moveCanBuild;

    public bool IsEditMode => isEditMode;
    public BuildingInstance CurrentEditingTarget
    {
        get
        {
            if (!isEditMode) return null;
            if (isMovingExisting && movingBuilding != null)
                return movingBuilding;
            return hoveredBuilding;
        }
    }

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

    private void SetupBuilding(GameObject obj)
    {         
        int buildingLayer = LayerMask.NameToLayer("Building");
        foreach (var tr in obj.GetComponentsInChildren<Transform>(true))
            tr.gameObject.layer = buildingLayer;
    }

    public void StartPlacement(ArcData arc, ArcRecipe arcRecipe, GameObject previewPrefab)
    {

        if (!BuildManager.I.HasMaterials(arcRecipe) && !BuildManager.I.debugMode)
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance.Show("재료가 부족합니다.");
            return;
        }

        CancelPlacement();

        currentArc = arc;

        var prefab = arc.previewPrefab != null ? arc.previewPrefab : previewPrefab;

        previewInstance = Instantiate(prefab);
        SetupPreview(previewInstance);
        previewVisual = previewInstance.GetComponent<BuildPreviewVisual>();

        SoundManager.I?.PlayUIClick();

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
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.cKey.wasPressedThisFrame && !isPlacing)
        {
            // C로 편집 모드 토글 (설치 프리뷰 중일 땐 토글 안 함)
            //UIManager.Instance?.HideUI(UIType.Building);
            if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
              return;
            UIManager.Instance?.ToggleUI(UIType.EditBuild);
            //isEditMode = !isEditMode;

            if (!isEditMode)
            {
                ExitEditMode();
            }
        }

        // === 1) 설치 프리뷰 모드 ===
        if (isPlacing && currentArc != null && previewInstance != null)
        {
            Vector3 targetPos;
            Quaternion targetRot;

            GetTargetTransform(out targetPos, out targetRot);

            previewInstance.transform.position = targetPos;
            previewInstance.transform.rotation = targetRot;

            lastCanBuild = BuildManager.I.CanBuildAt(currentArc, targetPos, targetRot, out lastError);
            previewVisual?.SetValid(lastCanBuild);

            HandleInput(targetPos, targetRot);
            return;
        }

        // === 2) 건축 편집 모드 ===
        if (isEditMode)
        {
            UpdateEditMode();
        }
    }

    private void UpdateEditMode()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null) return;
        if (!isMovingExisting)
        {
            // 아직 아무것도 안 옮기는 상태 → 바라보는 건물 찾기 & 이동/삭제 입력
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            hoveredBuilding = null;

            if (Physics.Raycast(ray, out var hit, editPickDistance, buildingMask))
            {
                hoveredBuilding = hit.collider.GetComponentInParent<BuildingInstance>();
                if (hoveredBuilding == null)
                {
                    hoveredBuilding = hit.collider.GetComponent<BuildingInstance>();
                }
            }
            //Debug.Log($"Hovered Building: {hoveredBuilding}");
            previewVisual?.ResetColor();
            previewVisual = hoveredBuilding?.gameObject.GetComponent<BuildPreviewVisual>();

            // if (previewVisual != null)
            // {
            //     previewVisual.SetEditMode();
            // }
            if (outlineToggle) 
                outlineToggle.SetOutlined(false);
            outlineToggle = hoveredBuilding?.gameObject.GetComponent<OutlineToggle>();
            if (outlineToggle) 
                outlineToggle.SetOutlined(true, true);

            // TODO: 여기서 hoveredBuilding 에 하이라이트 켜주면 좋음 (InteractionHighlight 등)

            // 왼쪽 클릭 → 이동 모드 시작
            if (hoveredBuilding != null && mouse.leftButton.wasPressedThisFrame)
            {
                SetupPreview(hoveredBuilding.gameObject);
                StartMoveExisting(hoveredBuilding);
            }

            // Delete 키 → 삭제
            if (hoveredBuilding != null &&
                keyboard != null && keyboard.deleteKey.wasPressedThisFrame)
            {
                if (outlineToggle)
                    outlineToggle.SetOutlined(false);
                BuildManager.I.TryRemoveBuilding(hoveredBuilding);
                hoveredBuilding = null;
                previewVisual = null;
                outlineToggle = null;
            }
        }
        else
        {
            // 이미 선택한 건물 이동 중
            UpdateMovingExisting(mouse, keyboard);
        }
    }

    private void ExitEditMode()
    {
        if (isMovingExisting && movingBuilding != null)
        {
            // 편집 모드 끌 때 이동 중이었으면 원위치로
            SetupBuilding(movingBuilding.gameObject);
            movingBuilding.transform.SetPositionAndRotation(moveStartPos, moveStartRot);
        }

        previewVisual?.ResetColor();
        if (outlineToggle != null && hoveredBuilding != null)
        {
            outlineToggle.SetOutlined(false);
        }
        isMovingExisting = false;
        movingBuilding = null;
        hoveredBuilding = null;
        previewInstance = null;
        previewVisual = null;
        currentArc = null;

        // 하이라이트 끄기 등 추가 정리
        //previewVisual.ResetColor();
    }


    private void StartMoveExisting(BuildingInstance inst)
    {
        movingBuilding = inst;
        moveStartPos = inst.transform.position;
        moveStartRot = inst.transform.rotation;

        // 이 건물이 어떤 ArcData인지 DB에서 조회
        if (!BuildManager.I.ArcDB.TryGet(inst.ArcId, out var arc))
        {
            Debug.LogWarning($"ArcData not found for building {inst.ArcId}");
            movingBuilding = null;
            return;
        }

        currentArc = arc;           // GetTargetTransform / CanBuildAt 에서 사용
        isMovingExisting = true;

        // 프리뷰 색 변경하고 싶으면 여기서 BuildPreviewVisual 사용
        previewInstance = movingBuilding.gameObject;
        previewVisual = previewInstance.GetComponent<BuildPreviewVisual>();
        if (previewVisual != null)
        {
            previewVisual.SetValid(true); // 일단 초록색 등
        }
    }

    private void UpdateMovingExisting(Mouse mouse, Keyboard keyboard)
    {
        // 새 위치/회전은 기존 설치 프리뷰와 똑같이 계산
        Vector3 pos;
        Quaternion rot;
        GetTargetTransform(out pos, out rot);

        movingBuilding.transform.SetPositionAndRotation(pos, rot);

        // 새 위치가 유효한지 BuildManager 규칙 재사용
        //moveCanBuild = BuildManager.I.CanBuildAt(currentArc, pos, rot, out moveError, true);
        moveCanBuild = BuildManager.I.CanMoveAt(movingBuilding, pos, rot, out moveError);
        previewVisual?.SetValid(moveCanBuild);

        // 왼쪽 클릭 → 이동 확정
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (moveCanBuild)
            {
                if (BuildManager.I.TryMoveBuilding(movingBuilding, pos, rot, out moveError))
                {
                    SetupBuilding(movingBuilding.gameObject);
                    FinishMoveExisting();
                }
                else
                {
                    ToastMessageUI.Instance?.Show($"이동 실패: {moveError}");
                }
            }
            else
            {
                ToastMessageUI.Instance?.Show($"이동 불가: {moveError}");
            }
        }

        // 우클릭 또는 ESC → 이동 취소 (원위치)
        if (mouse.rightButton.wasPressedThisFrame ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
        {
            movingBuilding.transform.SetPositionAndRotation(moveStartPos, moveStartRot);
            SetupBuilding(movingBuilding.gameObject);
            FinishMoveExisting();
        }
    }

    private void FinishMoveExisting()
    {
        if (outlineToggle != null && hoveredBuilding != null)
        {
            outlineToggle.SetOutlined(false);
        }
        isMovingExisting = false;
        movingBuilding = null;
        previewVisual?.ResetColor();
        previewInstance = null;
        previewVisual = null;
        currentArc = null;
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

        // PC일 때만 마우스 입력을 받게 하고 싶으면:
        bool isMobile = PlatformManager.Instance != null
            ? PlatformManager.Instance.IsMobile
            : Application.isMobilePlatform;

        if (!isMobile)
        {
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                TryConfirmBuild();
        }

        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
        {
            TryCancelBuild();
        }
    }


    public void SetEditMode(bool editMode)
    {
        isEditMode = editMode;
        if (!isEditMode)
        {
            ExitEditMode();
        }
    }

    public void TryConfirmBuild()
    {
        if (!isPlacing || currentArc == null || previewInstance == null)
            return;

        if (!lastCanBuild)
        {
            ToastMessageUI.Instance?.Show($"설치 불가: {lastError}");
            return;
        }

        Vector3 spawnPos = lastValidPos;
        Quaternion spawnRot = lastRot;

        if (Physics.Raycast(lastValidPos + Vector3.up * 5f, Vector3.down, out var hit, 20f, groundMask))
            spawnPos = hit.point;

        bool built = BuildManager.I.TryBuild(currentArc.arcId, spawnPos, spawnRot);
        if (built)
            CancelPlacement();
    }

    public void TryCancelBuild()
    {
        if (!isPlacing) return;
        CancelPlacement();
    }

}