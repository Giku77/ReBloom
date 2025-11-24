using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildInput : MonoBehaviour
{
    [SerializeField] private InputAction buildAction;
    [SerializeField] private InputAction toggleBuildUIAction;

    [SerializeField] private InputAction debugResearchPointAction;

    [SerializeField] private InputAction addMouse;
    [SerializeField] private InputAction subMouse;

    [SerializeField] private BuildUI buildUI;   
    private GameObject player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnEnable()
    {
        buildAction.Enable();
        buildAction.performed += OnBuild;
        toggleBuildUIAction.Enable();
        toggleBuildUIAction.performed += OnToggleBuildUI;
        debugResearchPointAction.Enable();
        debugResearchPointAction.performed += OnDebugAddResearchPoint;
        addMouse.Enable();
        addMouse.performed += OnDebugAddMouse;
        subMouse.Enable();
        subMouse.performed += OnDebugSubMouse;

    }

    private void OnDisable()
    {
        buildAction.performed -= OnBuild;
        buildAction.Disable();
        toggleBuildUIAction.performed -= OnToggleBuildUI;
        toggleBuildUIAction.Disable();
        debugResearchPointAction.performed -= OnDebugAddResearchPoint;
        debugResearchPointAction.Disable();
        addMouse.performed -= OnDebugAddMouse;
        addMouse.Disable();
        subMouse.performed -= OnDebugSubMouse;
        subMouse.Disable();
    }

    private void OnBuild(InputAction.CallbackContext ctx)
    {
        Debug.Log("Build Input Received");
        var buildId = QuestManager.I.Current.goals[0].objectId;
        //Debug.Log($"Trying to build ID: {buildId}");
        var playerPos = player.transform.position;
        playerPos += player.transform.forward * 2.0f;
        BuildManager.I?.TryBuild(buildId, playerPos, Quaternion.identity);
    }
    private void OnToggleBuildUI(InputAction.CallbackContext ctx)
    {
        Debug.Log("Toggle Build UI Input Received");
        if (buildUI != null)
        {
            buildUI.Toggle();
        }
    }

    private void OnDebugAddResearchPoint(InputAction.CallbackContext ctx)
    {
        ResearchManager.I.DebugFillToMax();
    }

    private void OnDebugAddMouse(InputAction.CallbackContext ctx)
    {
        var camera = Camera.main.GetComponent<ThirdPersonCamera>();
        camera.AddMouseSensitivity(1f);
    }

    private void OnDebugSubMouse(InputAction.CallbackContext ctx)
    {
        var camera = Camera.main.GetComponent<ThirdPersonCamera>();
        camera.SubMouseSensitivity(1f);
    }
}