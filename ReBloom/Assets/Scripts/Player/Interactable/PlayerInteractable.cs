using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 5f; //상호작용 가능한 범위
    [SerializeField] private float interactRadius = 2f; //상호작용 가능한 넓이
    [SerializeField] private LayerMask interactLayer;

    PlayerController player;

    private CancellationTokenSource cts;

    private bool isInteractive = false;
    private InteractionHighlight currentHighlight = null; // 현재 하이라이트된 오브젝트


    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    private void CancelInteract()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private void Update()
    {
        CheckForInteractable();
    }

    //private async UniTask StartInteract()
    //{
    //    try
    //    {
    //        if (TryInteract())
    //            return;
    //        await UniTask.Yield(PlayerLoopTiming.Update, cts);

    //    }
    //    catch (System.Exception e)
    //    {
    //        CancelInteract();
    //    }
    //}

    private bool TryInteract()
    {
        if (Physics.SphereCast(transform.position, interactRadius, Camera.main.transform.forward, out RaycastHit hit, interactRange, interactLayer))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact(player);

                return true;
            }
        }

        return false;
    }

    private void CheckForInteractable()
    {
        if (Physics.SphereCast(transform.position, interactRadius, Camera.main.transform.forward, out RaycastHit hit, interactRange, interactLayer))
        {
            if (hit.collider.TryGetComponent<InteractionHighlight>(out var highlight))
            {
                if (currentHighlight != highlight)
                 {
                    if (currentHighlight != null)
                    {
                        currentHighlight.Hide();
                    }
                    currentHighlight = highlight;
                    currentHighlight.Show();
                }
            }
        }
        else
        {
            if (currentHighlight != null)
            {
                currentHighlight.Hide();
                currentHighlight = null;
            }
        }
    }
}
