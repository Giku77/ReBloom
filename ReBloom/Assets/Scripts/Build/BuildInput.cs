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
        var buildId = QuestManager.I.Current.goals[0].objectId;
        //Debug.Log($"Trying to build ID: {buildId}");
        var playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;
        playerPos += GameObject.FindGameObjectWithTag("Player").transform.forward * 2.0f;
        BuildManager.I?.TryBuild(buildId, playerPos, Quaternion.identity);
    }
}
