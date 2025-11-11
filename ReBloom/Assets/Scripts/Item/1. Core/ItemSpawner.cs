using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class ItemSpawner : MonoBehaviour
{
    /// <summary>
    /// 월드에 아이템 스폰 (비동기)
    /// </summary>

    [Header("Spawn Settings")]
    [SerializeField] private Transform itemParent; // 생성된 아이템들의 부모 (씬 정리용)
    public async Task<GameObject> SpawnItemInWorld(int itemID, Vector3 position)
    {
        // 1. ItemDatabase에서 아이템 데이터 가져오기
        ItemBase itemData = ItemDatabase.I.GetItem(itemID);
        if (itemData == null)
        {
            Debug.LogError($"[ItemSpawner] 아이템 데이터 없음: {itemID}");
            return null;
        }

        // 2. Addressable 프리팹 로드 (비동기 대기)
        GameObject prefab = await LoadItemPrefabAsync(itemData);
        if (prefab == null)
        {
            Debug.LogError($"[ItemSpawner] 프리팹 로드 실패: {itemData.itemName}");
            return null;
        }

        // 3. 월드에 GameObject 생성
        GameObject itemObj = Instantiate(prefab, position, Quaternion.identity, itemParent);

        // 4. WorldItem 컴포넌트에 데이터 연결
        var worldItem = itemObj.GetComponent<WorldItem>();
        worldItem?.Initialize(itemData);

        return itemObj;
    }
    public async Task<GameObject> SpawnItemInWorld(ItemBase data, Vector3 position)
    {
        if (data == null)
        {
            Debug.LogError($"[ItemSpawner] 아이템 데이터 없음: {data.itemID}");
            return null;
        }

        // 2. Addressable 프리팹 로드 (비동기 대기)
        GameObject prefab = await LoadItemPrefabAsync(data);
        if (prefab == null)
        {
            Debug.LogError($"[ItemSpawner] 프리팹 로드 실패: {data.itemName}");
            return null;
        }

        // 3. 월드에 GameObject 생성
        GameObject itemObj = Instantiate(prefab, position, Quaternion.identity, itemParent);

        // 4. WorldItem 컴포넌트에 데이터 연결
        var worldItem = itemObj.GetComponent<WorldItem>();
        worldItem?.Initialize(data);

        return itemObj;
    }

    /// <summary>
    /// Addressable 프리팹 비동기 로드
    /// </summary>
    private async Task<GameObject> LoadItemPrefabAsync(ItemBase itemData)
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(itemData.worldPrefabAddress);
            GameObject prefab = await handle.Task;

            Debug.Log($"[ItemSpawner] 프리팹 로드 성공: {prefab.name}");
            return prefab;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemSpawner] 프리팹 로드 실패: {itemData.itemName}\n{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 드롭 아이템 (물리 효과 포함)
    /// </summary>
    public async Task<GameObject> DropItem(int itemID, Vector3 position, Vector3 force)
    {
        GameObject itemObj = await SpawnItemInWorld(itemID, position);

        if (itemObj != null && itemObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        return itemObj;
    }
    public async Task<GameObject> DropItem(ItemBase data, Vector3 position, Vector3 force)
    {
        GameObject itemObj = await SpawnItemInWorld(data, position);

        if (itemObj != null && itemObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        return itemObj;
    }

    // ===== 테스트 코드 =====
    //public InputActionReference spawnActionRef;

    //public async void TestSpawn(InputAction.CallbackContext ctx)
    //{
    //    if (ctx.performed)
    //    {
    //        await DropItem(4001001, new Vector3(0, 5, 0), new Vector3(0, 20, 0));
    //        Debug.Log("[ItemSpawner] 아이템 스폰 완료!");
    //    }
    //}

    //private void OnEnable()
    //{
    //    if (spawnActionRef != null)
    //        spawnActionRef.action.performed += TestSpawn;
    //}

    //private void OnDisable()
    //{
    //    if (spawnActionRef != null)
    //        spawnActionRef.action.performed -= TestSpawn;
    //}
}