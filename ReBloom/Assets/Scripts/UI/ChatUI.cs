using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Text;
using UnityEngine.EventSystems;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference chatToggleAction;
    [SerializeField] private bool createDefaultAction = true;

    [Header("Chat Mode Settings")]
    [SerializeField] private bool hideCursorInGameMode = true;

    [Header("Display Settings")]
    [SerializeField] private int maxDisplayedMessages = 50;
    [SerializeField] private bool autoScroll = true;
    [SerializeField] private Color systemMessageColor = Color.yellow;
    [SerializeField] private Color playerMessageColor = Color.white;

    private bool isChatMode = false;

    private Coroutine bindCo;
    private Coroutine focusCo;

    private bool blockEnterUntilRelease = false;


    private void OnEnable()
    {
        Debug.Log("[ChatUI] OnEnable 시작");

        if (ChatManager.I != null)
        {
            ChatManager.I.Messages.OnListChanged += OnMessagesChanged;
            Refresh();
        }
        else
        {
            bindCo = StartCoroutine(BindChatManagerWhenReady());
        }
    }


    private System.Collections.IEnumerator BindChatManagerWhenReady()
    {
        while (ChatManager.I == null)
            yield return null;

        ChatManager.I.Messages.OnListChanged += OnMessagesChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (bindCo != null) StopCoroutine(bindCo);

        if (ChatManager.I != null)
        {
            ChatManager.I.Messages.OnListChanged -= OnMessagesChanged;
        }
    }

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendClicked);
        }

        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnInputSubmit);
        }

        // 시작 시 채팅 모드 비활성화
        SetChatMode(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        bool enterPressed =
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame;

        bool enterHeld =
            Keyboard.current.enterKey.isPressed ||
            Keyboard.current.numpadEnterKey.isPressed;

        if (blockEnterUntilRelease)
        {
            if (!enterHeld)
                blockEnterUntilRelease = false;

            return;
        }

        if (!isChatMode && enterPressed)
        {
            SetChatMode(true);
        }
    }

    private void LateUpdate()
    {
        if (!isChatMode) return;
        if (inputField == null) return;
        if (EventSystem.current == null) return;

        // 현재 선택된 오브젝트가 inputField가 아니면 포커스 복구
        if (EventSystem.current.currentSelectedGameObject != inputField.gameObject)
        {
            // 설정 UI가 열려있는 중에는 복구하면 안 될 수 있으니 조건 추가 가능
            inputField.ActivateInputField();
            inputField.Select();
        }
    }



    private void OnChatToggle(InputAction.CallbackContext context)
    {
        if (isChatMode) return;
        Debug.Log($"[ChatUI] OnChatToggle 호출됨! Phase: {context.phase}, Value: {context.ReadValueAsButton()}");
        ToggleChatMode();
    }

    private void ToggleChatMode()
    {
        SetChatMode(!isChatMode);
    }

    private void SetChatMode(bool enabled)
    {
        isChatMode = enabled;

        if (focusCo != null)
        {
            StopCoroutine(focusCo);
            focusCo = null;
        }

        UIManager.Instance?.SetBlockingInput(isChatMode);

        if (isChatMode)
        {
            // 채팅 모드 활성화
            if (hideCursorInGameMode)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (inputField != null)
            {
                // 강제로 포커스 설정
                focusCo = StartCoroutine(FocusInputField());
            }

            Debug.Log("[ChatUI] 채팅 모드 활성화 - 마우스 표시");
        }
        else
        {
            // 게임 모드로 복귀
            if (hideCursorInGameMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (inputField != null)
            {
                inputField.DeactivateInputField();
            }

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            Debug.Log("[ChatUI] 게임 모드로 복귀 - 마우스 숨김");
        }
    }

    private System.Collections.IEnumerator FocusInputField()
    {
        // 한 프레임 대기 후 포커스
        yield return null;
        
        if (inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
            Debug.Log("[ChatUI] InputField에 포커스 설정됨");
        }
    }

    private void OnMessagesChanged(Unity.Netcode.NetworkListEvent<ChatMessage> changeEvent)
    {
        Refresh();
    }

    public void OnSendClicked()
    {
        if (ChatManager.I == null) return;
        if (inputField == null) return;

        if (!string.IsNullOrWhiteSpace(inputField.text))
        {
            ChatManager.I.TrySend(inputField.text);
            inputField.text = "";
        }

        SetChatMode(false);
    }

    private void OnInputSubmit(string text)
    {
        if (!isChatMode) return;

        blockEnterUntilRelease = true;

        if (!string.IsNullOrWhiteSpace(text))
        {
            OnSendClicked();
        }
        else
        {
            SetChatMode(false);
        }
    }

    private void Refresh()
    {
        if (ChatManager.I == null || logText == null) return;

        var sb = new StringBuilder();
        int count = ChatManager.I.Messages.Count;
        int startIndex = Mathf.Max(0, count - maxDisplayedMessages);

        for (int i = startIndex; i < count; i++)
        {
            var m = ChatManager.I.Messages[i];
            
            // 시스템 메시지인지 체크
            if (m.SenderClientId == ulong.MaxValue)
            {
                sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(systemMessageColor)}>");
                sb.Append($"[System] {m.Text}");
                sb.Append("</color>");
            }
            else
            {
                string serializedName = m.SenderName.ToString();
                string name = !string.IsNullOrWhiteSpace(serializedName)
                    ? serializedName
                    : (PlayerRegistry.I != null
                        ? PlayerRegistry.I.GetName(m.SenderClientId)
                        : $"Player#{m.SenderClientId}");

                sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(playerMessageColor)}>");
                sb.Append($"{name}: {m.Text}");
                sb.Append("</color>");
            }

            if (i < count - 1)
            {
                sb.AppendLine();
            }
        }

        logText.text = sb.ToString();

        // 자동 스크롤
        if (autoScroll && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
        }

        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(OnInputSubmit);
        }
    }
}
