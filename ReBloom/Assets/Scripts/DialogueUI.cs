using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueUI : UIBase  
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image characterImage;
    [SerializeField] private float typeSpeed = 40f; // 글자/초

    private bool nextRequested;

    public void OnNextInput(InputAction.CallbackContext ctx)
    {
        //if (!ctx.started) return;
        //Debug.Log("DialogueUI: OnNextInput received");
        nextRequested = true;
    }

    public async UniTask ShowLineAsync(
        string localizedText,
        bool showCharacterImage = false,
        bool waitForNextInput = true,
        bool showNextHint = true)
    {
        Show();

        if (characterImage != null)
            characterImage.gameObject.SetActive(showCharacterImage);

        var token = this.GetCancellationTokenOnDestroy();

        nextRequested = false;
        messageText.text = "";

        foreach (char c in localizedText)
        {
            messageText.text += c;

            if (nextRequested)
                break;

            await UniTask.Delay(
                (int)(1000f / typeSpeed),
                cancellationToken: token);
        }

        messageText.text = localizedText;

        if (!waitForNextInput)
        {
            nextRequested = false;
            return;
        }

        if (showNextHint)
        {
            messageText.text = localizedText +
                               " <color=#FFA500>[G] 다음</color>";
        }

        nextRequested = false;
        await UniTask.WaitUntil(() => nextRequested, cancellationToken: token);
        nextRequested = false;
    }
}
