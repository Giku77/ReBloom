using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class BuildInput : MonoBehaviour
{
    [SerializeField] private InputAction toggleBuildUIAction;

    [SerializeField] private InputAction addMouse;
    [SerializeField] private InputAction subMouse;
    [SerializeField] private InputAction debugBuildingModeAction;

    [SerializeField] private BuildUI buildUI;
    private GameObject player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnEnable()
    {
        toggleBuildUIAction.Enable();
        toggleBuildUIAction.performed += OnToggleBuildUI;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        addMouse.Enable();
        addMouse.performed += OnDebugAddMouse;
        subMouse.Enable();
        subMouse.performed += OnDebugSubMouse;
        debugBuildingModeAction.Enable();
        debugBuildingModeAction.performed += OnDebugBuildingMode;
#endif
    }

    private void OnDisable()
    {
        toggleBuildUIAction.performed -= OnToggleBuildUI;
        toggleBuildUIAction.Disable();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        addMouse.performed -= OnDebugAddMouse;
        addMouse.Disable();
        subMouse.performed -= OnDebugSubMouse;
        subMouse.Disable();
        debugBuildingModeAction.performed -= OnDebugBuildingMode;
        debugBuildingModeAction.Disable();
#endif
    }

    private void OnToggleBuildUI(InputAction.CallbackContext ctx)
    {
        Debug.Log("Toggle Build UI Input Received");
        if (buildUI != null)
        {
            Debug.Log("Toggling Build UI");
            buildUI.Toggle();
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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

    private void OnDebugBuildingMode(InputAction.CallbackContext ctx)
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && !nm.IsServer)
        {
            ToastMessageUI.Instance?.Show("디버그 빌딩 모드는 호스트만 사용할 수 있습니다.");
            return;
        }

        ToastMessageUI.Instance?.Show("디버그 빌딩 모드 토글");
        ResearchManager.I?.DebugFillToMax();
        BuildManager.I?.ToggleDebugBuildingMode();
    }
#endif
}
