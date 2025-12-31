using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuestInput : MonoBehaviour
{
    public InputAction questCompleteAction;
    [SerializeField] private Button questCompleteButton;

    private void OnEnable()
    {
        questCompleteAction.Enable();
        questCompleteAction.performed += OnQuestComplete;
        if (questCompleteButton != null)
        {
            questCompleteButton.onClick.AddListener(OnQuestComplete);
        }
    }

    private void OnDisable()
    {
        questCompleteAction.performed -= OnQuestComplete;
        questCompleteAction.Disable();
        if (questCompleteButton != null)
        {
            questCompleteButton.onClick.RemoveListener(OnQuestComplete);
        }
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

    private void Update()
    {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            QuestManager.I?.DebugForceCompleteAndGoNext();
        }
    #endif
    }


}
