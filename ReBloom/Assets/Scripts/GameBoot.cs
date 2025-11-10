using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameBoot : MonoBehaviour
{
    //[SerializeField] private string[] baseScenes;   // 필수 씬
    //[SerializeField] private string[] optionalScenes; // 선택 씬

    public SceneLoader SceneLoader;

    private ThirdPersonCamera camCtrl;
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
            Debug.LogError("Player를 찾지 못했습니다.");
            return;
        }

        var player = GameObject.FindWithTag("Player");
       
        var playerInput = player.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("Player에 PlayerInput 컴포넌트가 없습니다.");
            return;
        }

        camCtrl = Camera.main.GetComponent<ThirdPersonCamera>();
        if (camCtrl == null)
        {
            Debug.LogError("메인 카메라에 ThirdPersonCamera 컴포넌트가 없습니다.");
            return;
        }

        var lookAction = playerInput.actions["Look"];
        lookAction.performed += camCtrl.OnLook;
        lookAction.canceled += camCtrl.OnLook;

        playerInput.camera = Camera.main;
        Debug.Log("PlayerInput과 카메라 바인딩 완료");
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
        if (playerInput != null && camCtrl != null)
        {
            var look = playerInput.actions["Look"];
            look.performed -= camCtrl.OnLook;
            look.canceled -= camCtrl.OnLook;
        }
    }
}
