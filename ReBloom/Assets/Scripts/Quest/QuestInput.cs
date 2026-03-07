using Unity.Netcode;
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
        var nqm = NetworkQuestManager.I;
        if (nqm == null) return;

        if (!nqm.IsAwaitingHostAdvance) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            nqm.RequestAdvanceFromHost();
    }

    public void OnQuestComplete()
    {
        var nqm = NetworkQuestManager.I;
        if (nqm == null) return;

        if (!nqm.IsAwaitingHostAdvance) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            nqm.RequestAdvanceFromHost();
    }

    private void Update()
    {
    //#if UNITY_EDITOR || DEVELOPMENT_BUILD
    //    if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
    //    {
    //        QuestManager.I?.DebugForceCompleteAndGoNext();
    //    }
    //#endif
    }


}
