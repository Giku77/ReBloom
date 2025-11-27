using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
    private CancellationTokenSource equipCts;
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

    /// <summary>
    /// 도구 카테고리별 스케일 적용
    /// </summary>
    private void ApplyToolScale(ToolCategory category)
    {
        if (currentToolInstance == null) return;

        switch (category)
        {
            case ToolCategory.Hammer:
                currentToolInstance.transform.localScale = hammerScale;
                break;
            case ToolCategory.Shovel:
                currentToolInstance.transform.localScale = shovelScale;
                break;
            default:
                currentToolInstance.transform.localScale = Vector3.one;
                break;
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

        // 이전 장착 취소
        equipCts?.Cancel();
        equipCts?.Dispose();
        equipCts = new CancellationTokenSource();

        UnequipToolInstance();

        string toolAddress = GetAddressableKey(tool);

        try
        {
            Debug.Log($"[ToolEquipManager] 도구 로드 시작: {toolAddress}");

            // 새로운 Handle로 로드
            var newHandle = Addressables.LoadAssetAsync<GameObject>(toolAddress);

            // 취소 토큰과 함께 대기
            await newHandle.ToUniTask(cancellationToken: equipCts.Token);

            // 취소되었는지 확인
            if (equipCts.Token.IsCancellationRequested)
            {
                Debug.Log($"[ToolEquipManager] 장착 취소됨: {toolAddress}");
                Addressables.Release(newHandle);
                return;
            }

            if (newHandle.Status == AsyncOperationStatus.Succeeded)
            {
                // 이전 Handle 정리
                ReleaseCurrentHandle();

                // 새 Handle 저장
                currentHandle = newHandle;

                currentToolInstance = Instantiate(newHandle.Result, toolHolder);

                ApplyToolScale(tool.toolCategory);

                Debug.Log($"[ToolEquipManager] 도구 장착 완료: {tool.itemName}");
            }
            else
            {
                Debug.LogError($"[ToolEquipManager] 도구 로드 실패: {toolAddress}");
                Addressables.Release(newHandle);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[ToolEquipManager] 장착 작업 취소됨: {toolAddress}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ToolEquipManager] 도구 로드 에러: {toolAddress}\n{e.Message}");
        }

        //if (tool == null)
        //{
        //    Debug.LogError("[ToolEquipManager] ToolItemData가 null입니다!");
        //    return;
        //}

        //if (toolHolder == null)
        //{
        //    Debug.LogError("[ToolEquipManager] ToolHolder가 없습니다!");
        //    return;
        //}

        //UnequipTool();

        //string toolAddress = GetAddressableKey(tool);

        //try
        //{
        //    Debug.Log($"[ToolEquipManager] 도구 로드 시작: {toolAddress}");

        //    currentHandle = Addressables.LoadAssetAsync<GameObject>(toolAddress);
        //    await currentHandle.Task;

        //    if (currentHandle.Status == AsyncOperationStatus.Succeeded)
        //    {
        //        currentToolInstance = Instantiate(currentHandle.Result, toolHolder);
        //        //currentToolInstance.transform.localPosition = Vector3.zero;
        //        //currentToolInstance.transform.localRotation = Quaternion.identity;


        //        Debug.Log($"[ToolEquipManager] 도구 장착 완료: {tool.itemName} ({toolAddress})");
        //    }
        //    else
        //    {
        //        Debug.LogError($"[ToolEquipManager] 도구 로드 실패: {toolAddress}");
        //    }
        //}
        //catch (System.Exception e)
        //{
        //    Debug.LogError($"[ToolEquipManager] 도구 로드 에러: {toolAddress}\n{e.Message}");
        //}
    }

    /// <summary>
    /// Handle만 정리
    /// </summary>
    private void ReleaseCurrentHandle()
    {
        if (currentHandle.IsValid())
        {
            Addressables.Release(currentHandle);
            Debug.Log("[ToolEquipManager] 이전 Handle 해제");
        }
    }

    /// <summary>
    /// 완전 해제 (외부 호출용)
    /// </summary>
    public void UnequipTool()
    {
        equipCts?.Cancel();
        equipCts?.Dispose();
        equipCts = null;

        UnequipToolInstance();
        ReleaseCurrentHandle();

        Debug.Log("[ToolEquipManager] 도구 완전 해제");

        //if (currentToolInstance != null)
        //{
        //    Destroy(currentToolInstance);
        //    currentToolInstance = null;
        //    Debug.Log("[ToolEquipManager] 도구 해제 완료");
        //}

        //if (currentHandle.IsValid())
        //{
        //    Addressables.Release(currentHandle);
        //}
    }

    /// <summary>
    /// 손에 들고 있는 도구 GameObject만 제거
    /// </summary>
    private void UnequipToolInstance()
    {
        if (currentToolInstance != null)
        {
            Destroy(currentToolInstance);
            currentToolInstance = null;
            //Debug.Log("[ToolEquipManager] 도구 인스턴스 제거");
        }
    }


    private string GetAddressableKey(ToolItemData tool)
    {
        string categoryName = "";

        switch (tool.toolCategory)
        {
            case ToolCategory.Shovel:
                categoryName = "Equip/Shovel";
                break;
            case ToolCategory.Hammer:
                categoryName = "Equip/Hammer";
                break;
            default:
                categoryName = "Equip/Tool";
                break;
        }

        return $"{categoryName}{tool.tier}";

        //string categoryName = "";

        //switch (tool.toolCategory)
        //{
        //    case ToolCategory.Shovel:
        //        categoryName = "Shovel";
        //        break;
        //    case ToolCategory.Hammer:
        //        categoryName = "Hammer";
        //        break;
        //    default:
        //        categoryName = "Tool";
        //        break;
        //}

        //return $"{categoryName}{tool.tier}";
    }
    
    private void OnDestroy()
    {
        UnequipTool();
    }
}