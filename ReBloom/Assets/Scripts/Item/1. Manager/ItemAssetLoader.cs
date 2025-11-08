using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class ItemAssetLoader : MonoBehaviour
{
    public Button addButton;
    public Button deleteButton;
    [SerializeField] private string prefabKey;

    private GameObject cachedPrefab;
    private List<GameObject> sceneGOList = new List<GameObject>();
    private AsyncOperationHandle<GameObject> handle;

    private async void Start()
    {
        handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
        try
        {
            cachedPrefab = await handle.Task;
        }
        catch
        {
            Debug.LogError($"Prefab 로드 실패: {prefabKey}");
            return;
        }

        addButton.onClick.AddListener(OnAdd);
        deleteButton.onClick.AddListener(OnDelete);
    }

    public void OnAdd()
    {
        if (cachedPrefab == null)
        {
            Debug.LogWarning("프리팹 아직 로드되지 않았거나 실패했습니다.");
            return;
        }

        var obj = Instantiate(cachedPrefab, Vector3.zero, Quaternion.identity);
        sceneGOList.Add(obj);
    }

    public void OnDelete()
    {
        if (sceneGOList.Count == 0)
        {
            Debug.LogWarning("삭제할 오브젝트가 없습니다.");
            return;
        }

        var last = sceneGOList[sceneGOList.Count - 1];
        sceneGOList.RemoveAt(sceneGOList.Count - 1);
        Destroy(last);
    }

    private void OnDestroy()
    {
        addButton.onClick.RemoveListener(OnAdd);
        deleteButton.onClick.RemoveListener(OnDelete);

       Addressables.Release(handle);

        sceneGOList.Clear();
    }
}
