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
        // 간단 버전: 플레이어 전방 Raycast → 히트 포인트에 배치
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, placeDistance, groundMask))
        {
            pos = hit.point;
        }
        else
        {
            // 못 맞추면 그냥 플레이어 앞에 고정 거리
            pos = playerTransform.position + playerTransform.forward * placeDistance;
        }

        //pos += Vector3.up * 1.1f; // 약간 띄우기
        rot = Quaternion.LookRotation(playerTransform.forward, Vector3.up);
        lastValidPos = pos;
        lastRot = rot;
    }

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