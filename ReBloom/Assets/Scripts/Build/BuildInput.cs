using UnityEngine;
using UnityEngine.InputSystem;

public class BuildInput : MonoBehaviour
{
    public InputAction buildAction;

    private void OnEnable()
    {
        buildAction.Enable();
        buildAction.performed += OnBuild;
    }

    private void OnDisable()
    {
        buildAction.performed -= OnBuild;
        buildAction.Disable();
    }

    private void OnBuild(InputAction.CallbackContext ctx)
    {
        Debug.Log("Build Input Received");
        BuildManager.I?.TryBuild(3101001, Vector3.zero, Quaternion.identity);
    }
}
