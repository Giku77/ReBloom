using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class GameStartSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private InventoryRobotPet robotPet;
    [SerializeField] private CutSceneManager cutSceneManager;

    private float startZoomDistance = 15f;
    private float targetZoomDistance = 2.5f;
    private float zoomDuration = 3f;
    private float initialDelay = 3f;

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer weatherAudio;
    private float originalVolume;
    private bool hasLocalPlayerBinding;

    private bool IsNetworkedSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        if (tutorialManager != null)
            tutorialManager.enabled = false;

        TryBindSceneReferences();
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
        TryBindExistingLocalPlayer();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
    }

    private async void Start()
    {
        if (weatherAudio != null)
            weatherAudio.GetFloat("WeatherVolume", out originalVolume);

        await EnsureRuntimeBindingsAsync();

        if (IsNetworkedSession && GameStartContext.StartMode == GameStartContext.Mode.Continue)
        {
            await UniTask.WaitUntil(() => MultiplayerSaveCoordinator.IsLoadFlowComplete);

            if (SaveManager.I != null && SaveManager.I.HasLoadedOnce)
            {
                AfterContinueLoaded();
                return;
            }
        }

        if (!IsNetworkedSession && GameStartContext.StartMode == GameStartContext.Mode.Continue)
        {
            bool loaded = false;
            if (SaveManager.I != null)
                loaded = await SaveManager.I.LoadAsync(GameStartContext.SlotId);

            if (loaded)
            {
                AfterContinueLoaded();
                return;
            }
        }

        if (GameStartContext.StartMode == GameStartContext.Mode.Debug)
        {
            SkipAllAndStartGameplay();
            return;
        }

        if (IsNetworkedSession)
        {
            cutSceneManager?.SetIntroCutsceneSeen(true);
            tutorialManager?.SetTutorialState(0, true);
            if (tutorialManager != null)
                tutorialManager.enabled = false;

            SkipAllAndStartGameplay();
            return;
        }

        await PlayNewGameFlow();
    }

    private async UniTask PlayNewGameFlow()
    {
        if (cutSceneManager != null && cutSceneManager.IntroCutsceneSeen)
        {
            SkipAllAndStartGameplay();
            StartTutorialIfNeeded();
            return;
        }

        await PlaySequence();
        StartTutorialIfNeeded();
    }

    private void AfterContinueLoaded()
    {
        cutSceneManager?.isDebugModeSkipCutScene();

        if (thirdPersonCamera != null)
            thirdPersonCamera.isSequenceLocked = false;

        if (playerController != null)
        {
            playerController.Anim.SetRootMotion(false);
            playerController.SetBlocked(false);
        }

        robotPet?.StopOrbitingPlayer();
        StartTutorialIfNeeded();
    }

    private void SkipAllAndStartGameplay()
    {
        cutSceneManager?.isDebugModeSkipCutScene();

        if (thirdPersonCamera != null)
            thirdPersonCamera.isSequenceLocked = false;

        if (playerController != null)
        {
            playerController.Anim.SetRootMotion(false);
            playerController.SetBlocked(false);
        }

        robotPet?.StopOrbitingPlayer();
    }

    private void StartTutorialIfNeeded()
    {
        if (tutorialManager == null) return;

        if (tutorialManager.IntroCompleted)
        {
            tutorialManager.enabled = false;
            return;
        }

        tutorialManager.enabled = true;
    }

    public async UniTask PlaySequence()
    {
        if (playerController == null)
        {
            Debug.LogWarning("[GameStartSequence] Local PlayerController was not found. Intro sequence is skipped.");
            SkipAllAndStartGameplay();
            StartTutorialIfNeeded();
            return;
        }

        playerController.Anim.PlaySleep();
        playerController.Anim.SetRootMotion(true);

        weatherAudio?.SetFloat("WeatherVolume", -80f);
        playerController.SetBlocked(true);

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.isSequenceLocked = true;
            SetCameraDistance(startZoomDistance);
            thirdPersonCamera.SetCameraAngle(180f, 40f);
        }

        if (cutSceneManager != null)
            await cutSceneManager.PlayCutSceneSequenceAsync(1301001);

        weatherAudio?.SetFloat("WeatherVolume", originalVolume);
        robotPet?.StartOrbitingPlayer(radius: 1.2f, speed: 80f);

        await UniTask.Delay((int)(initialDelay * 1000));

        if (thirdPersonCamera != null)
            await ZoomInCamera();

        playerController.Anim.PlayStandUp();
        await UniTask.Delay(5800);

        playerController.Anim.SetRootMotion(false);

        await LerpCameraAngle(40f, 10f, 1f);
        await UniTask.Delay(200);

        if (thirdPersonCamera != null)
            thirdPersonCamera.isSequenceLocked = false;

        robotPet?.StopOrbitingPlayer();
        playerController.SetBlocked(false);
    }

    private async UniTask ZoomInCamera()
    {
        float elapsed = 0f;
        float startDistance = startZoomDistance;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / zoomDuration);
            float currentDistance = Mathf.Lerp(startDistance, targetZoomDistance, t);
            SetCameraDistance(currentDistance);
            await UniTask.Yield();
        }

        SetCameraDistance(targetZoomDistance);
    }

    private void SetCameraDistance(float distance)
    {
        thirdPersonCamera?.SetDistance(distance);
    }

    private async UniTask LerpCameraAngle(float startPitch, float targetPitch, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            float currentPitch = Mathf.Lerp(startPitch, targetPitch, t);
            thirdPersonCamera?.SetCameraAngle(180f, currentPitch);
            await UniTask.Yield();
        }

        thirdPersonCamera?.SetCameraAngle(180f, targetPitch);
    }

    private async UniTask EnsureRuntimeBindingsAsync()
    {
        TryBindSceneReferences();

        if (!IsNetworkedSession)
            return;

        if (playerController != null)
        {
            hasLocalPlayerBinding = true;
            return;
        }

        await UniTask.WaitUntil(() => hasLocalPlayerBinding || playerController != null, cancellationToken: this.GetCancellationTokenOnDestroy());
        TryBindSceneReferences();
    }

    private void TryBindSceneReferences()
    {
        if (thirdPersonCamera == null)
            thirdPersonCamera = CameraRig.I != null ? CameraRig.I.ThirdPersonCamera : FindFirstObjectByType<ThirdPersonCamera>();

        if (cutSceneManager == null)
            cutSceneManager = FindFirstObjectByType<CutSceneManager>();

        if (!IsNetworkedSession)
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();
        }
        else
        {
            TryBindExistingLocalPlayer();
        }

        if (playerController != null)
        {
            hasLocalPlayerBinding = true;

            if (robotPet == null)
                robotPet = playerController.RobotPet;
        }
    }

    private void TryBindExistingLocalPlayer()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null)
            return;

        var localPlayerObject = nm.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObject == null)
            return;

        HandleLocalPlayerSpawned(localPlayerObject.gameObject);
    }

    private void HandleLocalPlayerSpawned(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        playerController = playerObject.GetComponent<PlayerController>();
        if (playerController == null)
            return;

        hasLocalPlayerBinding = true;

        if (robotPet == null)
            robotPet = playerController.RobotPet;
    }

    private void HandleLocalPlayerDespawned()
    {
        hasLocalPlayerBinding = false;
        playerController = null;
        robotPet = null;
    }
}