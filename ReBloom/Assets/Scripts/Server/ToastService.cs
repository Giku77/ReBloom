using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class ToastService : MonoBehaviour
{
    public static ToastService I { get; private set; }

    [SerializeField] private TextMeshProUGUI toastText;
    [SerializeField] private int defaultMs = 2000;

    private CancellationTokenSource cts;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;

        toastText.gameObject.SetActive(false);
    }

    public void Show(string msg, int ms = -1)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        toastText.gameObject.SetActive(true);
        toastText.text = msg;

        HideAfterAsync(ms < 0 ? defaultMs : ms, cts.Token).Forget();
    }

    private async UniTaskVoid HideAfterAsync(int ms, CancellationToken token)
    {
        try
        {
            await UniTask.Delay(ms, cancellationToken: token);
            toastText.gameObject.SetActive(false);
        }
        catch (OperationCanceledException) { }
    }
}
