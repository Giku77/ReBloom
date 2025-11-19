using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactLayer;

    PlayerController player;

    private CancellationTokenSource cts;

    private InteractionHighlight currentHighlight = null;

    private float currentHoldTime = 0;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        //TryInteract();

        if (context.started)
            StartInteract().Forget();
        else if (context.canceled)
            CancelInteract();
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

    private async UniTask StartInteract()
    {
        CancelInteract();
        cts = new CancellationTokenSource();

        try
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IInteractable>(out var interactable))
                {
                    Vector3 toTarget = hit.transform.position - transform.position;
                    float distance = toTarget.magnitude;
                    float dot = Vector3.Dot(transform.forward, toTarget.normalized);

                    if (dot > 0.5f && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }

            if (closestInteractable != null)
            {
                float holdTime = closestInteractable.HoldTime;
                if (holdTime > 0f)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(holdTime), cancellationToken: cts.Token);
                }
                closestInteractable.Interact(player);
            }
        }
        catch (System.OperationCanceledException)
        {
            CancelInteract();
        }

    }


    private bool TryInteract()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IInteractable>(out var interactable))
            {
                Vector3 toTarget = hit.transform.position - transform.position;
                float distance = toTarget.magnitude;
                float dot = Vector3.Dot(transform.forward, toTarget.normalized);

                if (dot > 0.5f && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != null)
        {
            closestInteractable.Interact(player);
            return true;
        }

        return false;
    }

    private void CheckForInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
        InteractionHighlight closestHighlight = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<InteractionHighlight>(out var highlight))
            {
                Vector3 toTarget = hit.transform.position - transform.position;
                float distance = toTarget.magnitude;
                float dot = Vector3.Dot(transform.forward, toTarget.normalized);

                if (dot > 0.5f && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestHighlight = highlight;
                }
            }
        }

        if (currentHighlight != closestHighlight)
        {
            currentHighlight?.Hide();
            currentHighlight = closestHighlight;
            currentHighlight?.Show();
        }
    }
}
