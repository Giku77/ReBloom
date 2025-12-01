using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueUI : UIBase  
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float typeSpeed = 40f; // 글자/초

    private bool nextRequested;

    public void OnNextInput(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        nextRequested = true;
    }

    public async UniTask ShowLineAsync(string localizedText)
    {
        Show();

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

        // 다음 키 입력 기다리기
        nextRequested = false;
        await UniTask.WaitUntil(() => nextRequested, cancellationToken: token);

        nextRequested = false;
        // 여기서 Hide()는 밖에서 컨트롤해도 되고, 마지막 대사 끝에서만 닫아도 됨
    }
}
