using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStartSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;


    private float startZoomDistance = 15f;
    private float targetZoomDistance = 3.2f;
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

        await UniTask.Delay((int)(initialDelay * 1000));

        if (thirdPersonCamera != null)
        {
            await ZoomInCamera();
        }

        playerController.Anim.PlayStandUp();

        await UniTask.Delay(7000);    

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.isSequenceLocked = false;
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
}