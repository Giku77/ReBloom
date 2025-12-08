using Cysharp.Threading.Tasks;
using UnityEngine;

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

    private void Awake()
    {
        if (tutorialManager != null)
        {
            tutorialManager.enabled = false;
        }
    }

    private async void Start()
    {
        await PlaySequence();
    }

    public async UniTask PlaySequence()
    {
        playerController.Anim.PlaySleep();
        playerController.Anim.SetRootMotion(true);

        if (playerController != null)
        {
            playerController.SetBlocked(true);
        }

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.isSequenceLocked = true;
            SetCameraDistance(startZoomDistance);
            thirdPersonCamera.SetCameraAngle(180f, 40f);
        }

        if (cutSceneManager != null) await cutSceneManager.PlayCutSceneSequenceAsync(1301001);

        if (robotPet != null)
        {
            robotPet.StartOrbitingPlayer(radius: 1.2f, speed: 80f);
        }

        await UniTask.Delay((int)(initialDelay * 1000));

        if (thirdPersonCamera != null)
        {
            await ZoomInCamera();
        }

        playerController.Anim.PlayStandUp();

        await UniTask.Delay(5800);

        playerController.Anim.SetRootMotion(false);

        await LerpCameraAngle(40f, 10f, 1f);

        await UniTask.Delay(200);    

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.isSequenceLocked = false;
        }

        if (robotPet != null)
        {
            robotPet.StopOrbitingPlayer();
        }

        if (tutorialManager != null)
        {
            tutorialManager.enabled = true;
        }
        else
        {
            if (playerController != null)
            {
                playerController.SetBlocked(false);
            }
        }
    }

    private async UniTask ZoomInCamera()
    {
        float elapsed = 0f;
        float startDistance = startZoomDistance;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;

            t = Mathf.SmoothStep(0, 1, t);

            float currentDistance = Mathf.Lerp(startDistance, targetZoomDistance, t);
            SetCameraDistance(currentDistance);

            await UniTask.Yield();
        }

        SetCameraDistance(targetZoomDistance);
    }

    private void SetCameraDistance(float distance)
    {
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.SetDistance(distance);
        }
    }

    private async UniTask LerpCameraAngle(float startPitch, float targetPitch, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t);

            float currentPitch = Mathf.Lerp(startPitch, targetPitch, t);
            thirdPersonCamera.SetCameraAngle(180f, currentPitch);

            await UniTask.Yield();
        }

        thirdPersonCamera.SetCameraAngle(180f, targetPitch);
    }
}