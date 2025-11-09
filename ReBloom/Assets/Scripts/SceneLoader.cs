using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string[] sceneAddresses;

    private readonly List<AsyncOperationHandle<SceneInstance>> _handles = new();

    public event Action onAllScenesLoaded;

    public void LoadAll()
    {
        foreach (var address in sceneAddresses)
        {
            var handle = Addressables.LoadSceneAsync(address, LoadSceneMode.Additive);
            _handles.Add(handle);

            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    Debug.Log($"¾À ·Îµå ¼º°ø: {address}");
                else
                    Debug.LogError($"¾À ·Îµå ½ÇÆÐ: {address}");
            };
        }
        onAllScenesLoaded?.Invoke();
    }

    public void LoadScene(string address)
    {
        var handle = Addressables.LoadSceneAsync(address, LoadSceneMode.Additive);
        _handles.Add(handle);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
                Debug.Log($"¾À ·Îµå ¼º°ø: {address}");
            else
                Debug.LogError($"¾À ·Îµå ½ÇÆÐ: {address}");
        };
    }

    public void UnloadAll()
    {
        foreach (var handle in _handles)
        {
            if (handle.IsValid())
                Addressables.UnloadSceneAsync(handle);
        }
        _handles.Clear();
    }
}
