using UnityEngine;
using UnityEngine.InputSystem;

public class QuestInput : MonoBehaviour
{
    public InputAction questCompleteAction;

    private void OnEnable()
    {
        questCompleteAction.Enable();
        questCompleteAction.performed += OnQuestComplete;
    }

    private void OnDisable()
    {
        questCompleteAction.performed -= OnQuestComplete;
        questCompleteAction.Disable();
    }

    private void OnQuestComplete(InputAction.CallbackContext ctx)
    {
        Debug.Log("Quest Complete Input Received");
        //QuestManager.I?.CompleteCurrent();
        QuestManager.I?.TryCompleteCurrent();
        //QuestManager.I?.PlayQuestCompleteAnimation();
    }

    public void OnQuestComplete()
    {
        Debug.Log("Quest Complete Input Received");
        //QuestManager.I?.CompleteCurrent();
        QuestManager.I?.TryCompleteCurrent();
        //QuestManager.I?.PlayQuestCompleteAnimation();
    }
}
