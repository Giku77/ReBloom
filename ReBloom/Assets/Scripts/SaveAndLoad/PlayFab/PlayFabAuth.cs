using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabAuth
{
    public static UniTask LoginAsync()
    {
        var tcs = new UniTaskCompletionSource();

        var req = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier, // PC/모바일 테스트용
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(req,
            _ => tcs.TrySetResult(),
            err => tcs.TrySetException(new System.Exception(err.GenerateErrorReport()))
        );

        return tcs.Task;
    }
}
