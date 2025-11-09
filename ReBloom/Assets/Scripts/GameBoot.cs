using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameBoot : MonoBehaviour
{
    //[SerializeField] private string[] baseScenes;   // 항상 로드
    //[SerializeField] private string[] optionalScenes; // 필요할 때만

    public SceneLoader SceneLoader;
    private void Start()
    {
        //foreach (var addr in baseScenes)
        //    SceneLoader.LoadScene(addr);
        SceneLoader.onAllScenesLoaded += OnAllScenesLoaded;
        SceneLoader.LoadAll();
    }

    private void OnAllScenesLoaded()
    {
        BindPlayerAfterLoad().Forget();
    }

    private async UniTaskVoid BindPlayerAfterLoad()
    {
        try
        {
            await UniTask
                .WaitUntil(() => GameObject.FindWithTag("Player") != null)
                .Timeout(TimeSpan.FromSeconds(5));   
        }
        catch (TimeoutException)
        {
            Debug.LogError("Player가 안 생김");
            return;
        }

        var player = GameObject.FindWithTag("Player");
       
        var playerInput = player.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("Player에 PlayerInput이 없습니다.");
            return;
        }

        var camCtrl = Camera.main.GetComponent<ThirdPersonCamera>();
        if (camCtrl == null)
        {
            Debug.LogError("부트씬 카메라에 ThirdPersonCamera 없어요");
            return;
        }

        var lookAction = playerInput.actions["Look"];
        lookAction.performed += camCtrl.OnLook;
        lookAction.canceled += camCtrl.OnLook;

        playerInput.camera = Camera.main;
        Debug.Log("PlayerInput에 부트씬 카메라 연결 완료");
    }

    private void OnDestroy()
    {
        if (SceneLoader != null)
        {
            SceneLoader.UnloadAll();
            SceneLoader.onAllScenesLoaded -= OnAllScenesLoaded;
        }

        var player = GameObject.FindWithTag("Player");
        var playerInput = player ? player.GetComponent<PlayerInput>() : null;
        var camCtrl = Camera.main.GetComponent<ThirdPersonCamera>();
        if (playerInput != null)
        {
            var look = playerInput.actions["Look"];
            look.performed -= camCtrl.OnLook;
            look.canceled -= camCtrl.OnLook;
        }
    }
}
