using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private HoldInteractionUI holdUI;

    PlayerController player;

    private CancellationTokenSource cts;

    private InteractionHighlight currentHighlight = null;
    private ToastMessageUI toastMessageUI;
    

    private void Awake()
    {
        player = GetComponent<PlayerController>();

        toastMessageUI = GameObject.FindWithTag("ToastMsg").GetComponent<ToastMessageUI>();
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

            if (closestInteractable != null && closestInteractable.CanInteract())
            {
                float holdTime = closestInteractable.HoldTime;
                if (holdTime > 0f)
                {
                    float elapsed = 0f;
                    holdUI.Show();
                    toastMessageUI.Show("채집 중....", holdTime);

                    while (elapsed < holdTime)
                    {
                        elapsed += Time.deltaTime;
                        float progress = elapsed / holdTime;
                        holdUI.UpdateProgress(progress);

                        await UniTask.Yield(cancellationToken: cts.Token);
                    }

                    toastMessageUI.Show("채집 완료!", 2f);
                    holdUI.Hide();
                }
                closestInteractable.Interact(player);
            }
        }
        catch (System.OperationCanceledException)
        {
            toastMessageUI.Show("채집 중단!", 2f);
            holdUI.Hide(); // 취소 시에도 UI 숨기기
            CancelInteract();
        }
    }

    private void CheckForInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
        InteractionHighlight closestHighlight = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.GetComponent<GatherObject>() != null)
                continue;

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
