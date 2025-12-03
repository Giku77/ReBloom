using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStartSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TutorialManager tutorialManager;

    [Header("Settings")]
    [SerializeField] private float initialDelay = 1f;

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
        if (playerController != null)
        {
            playerController.SetBlocked(true);
        }

        playerController.Anim.PlaySleep();

        await UniTask.Delay(5000);

        playerController.Anim.PlayStandUp();

        await UniTask.Delay((int)(initialDelay * 1000));

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
}
