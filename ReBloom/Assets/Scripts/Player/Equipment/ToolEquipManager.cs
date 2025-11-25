using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

public class ToolEquipManager : MonoBehaviour
{
    [Header("Tool Holder")]
    [SerializeField] private Transform toolHolder;
    
    [Header("Scale Settings")]
    [SerializeField] private Vector3 hammerScale = new Vector3(100f, 100f, 100f);
    [SerializeField] private Vector3 shovelScale = Vector3.one;
    
    [Header("Current Tool")]
    private GameObject currentToolInstance;
    private AsyncOperationHandle<GameObject> currentHandle;
    
    private void Start()
    {
        if (toolHolder == null)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            toolHolder = allTransforms.FirstOrDefault(t => t.name == "ToolHolder");
            
            if (toolHolder == null)
            {
                Debug.LogError("[ToolEquipManager] ToolHolder를 찾을 수 없습니다!");
            }
            else
            {
                Debug.Log($"[ToolEquipManager] ToolHolder 찾음: {toolHolder.name}");
            }
        }
    }
    
    public async void EquipTool(ToolItemData tool)
    {
        if (tool == null)
        {
            Debug.LogError("[ToolEquipManager] ToolItemData가 null입니다!");
            return;
        }
        
        if (toolHolder == null)
        {
            Debug.LogError("[ToolEquipManager] ToolHolder가 없습니다!");
            return;
        }
        
        UnequipTool();
        
        string toolAddress = GetAddressableKey(tool);
        
        try
        {
            Debug.Log($"[ToolEquipManager] 도구 로드 시작: {toolAddress}");
            
            currentHandle = Addressables.LoadAssetAsync<GameObject>(toolAddress);
            await currentHandle.Task;
            
            if (currentHandle.Status == AsyncOperationStatus.Succeeded)
            {
                currentToolInstance = Instantiate(currentHandle.Result, toolHolder);
                //currentToolInstance.transform.localPosition = Vector3.zero;
                //currentToolInstance.transform.localRotation = Quaternion.identity;

                
                Debug.Log($"[ToolEquipManager] 도구 장착 완료: {tool.itemName} ({toolAddress})");
            }
            else
            {
                Debug.LogError($"[ToolEquipManager] 도구 로드 실패: {toolAddress}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ToolEquipManager] 도구 로드 에러: {toolAddress}\n{e.Message}");
        }
    }

    public void UnequipTool()
    {
        if (currentToolInstance != null)
        {
            Destroy(currentToolInstance);
            currentToolInstance = null;
            Debug.Log("[ToolEquipManager] 도구 해제 완료");
        }
        
        if (currentHandle.IsValid())
        {
            Addressables.Release(currentHandle);
        }
    }
    

    private string GetAddressableKey(ToolItemData tool)
    {
        string categoryName = "";
        
        switch (tool.toolCategory)
        {
            case ToolCategory.Shovel:
                categoryName = "Shovel";
                break;
            case ToolCategory.Pickaxe:
                categoryName = "Hammer";
                break;
            case ToolCategory.Bag:
                categoryName = "Bag";
                break;
            default:
                categoryName = "Tool";
                break;
        }
        
        return $"{categoryName}{tool.tier}";
    }
    
    private void OnDestroy()
    {
        UnequipTool();
    }
}