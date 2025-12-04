using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameBoot : MonoBehaviour
{
    //[SerializeField] private string[] baseScenes;   // �ʼ� ��
    //[SerializeField] private string[] optionalScenes; // ���� ��

    public GameObject player;

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
            Debug.LogError("Player�� ã�� ���߽��ϴ�.");
            return;
        }

        var player = GameObject.FindWithTag("Player");
       
        var playerInput = player.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("Player�� PlayerInput ������Ʈ�� �����ϴ�.");
            return;
        }

        camCtrl = Camera.main.GetComponent<ThirdPersonCamera>();
        if (camCtrl == null)
        {
            Debug.LogError("���� ī�޶� ThirdPersonCamera ������Ʈ�� �����ϴ�.");
            return;
        }

        player.transform.position = Vector3.zero;

        var lookAction = playerInput.actions["Look"];
        lookAction.performed += camCtrl.OnLook;
        lookAction.canceled += camCtrl.OnLook;

        playerInput.camera = Camera.main;
        Debug.Log("PlayerInput�� ī�޶� ���ε� �Ϸ�");
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
