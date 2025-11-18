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
    [SerializeField] private ToastMessageUI toast;   

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

    public void StartPlacement(ArcData arc, ArcRecipe arcRecipe, GameObject previewPrefab)
    {

        if (!BuildManager.I.HasMaterials(arcRecipe))
        {
            toast.Show("재료가 부족합니다.");
            return;
        }

        CancelPlacement();

        currentArc = arc;


        previewInstance = Instantiate(previewPrefab);
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

        pos += Vector3.up * 1.5f; // 약간 띄우기
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
                bool built = BuildManager.I.TryBuild(currentArc.arcId, pos, rot);
                if (built)
                    CancelPlacement();
            }
            else
            {
                toast?.Show($"설치 불가: {lastError}");
            }
        }

        if (mouse.rightButton.wasPressedThisFrame ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
        }
    }
}