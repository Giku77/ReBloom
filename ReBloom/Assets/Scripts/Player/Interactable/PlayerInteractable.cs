using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Experimental.GraphView.GraphView;

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

    public static readonly string gathering = "Gather";
    public static readonly string pickUp = "PickUp";

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
        var msg = string.Empty;
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

                bool isWorldItem = closestInteractable is WorldItem;
                if (isWorldItem)
                {
                    player.Animator.SetTrigger(pickUp);
                    player.isInteracting = true;
                    await UniTask.Delay(800);
                }

                if (holdTime > 0f)
                {
                    player.isInteracting = true;
                    float elapsed = 0f;
                    holdUI.Show();
                    bool isGatherObject = closestInteractable is GatherObject;
                    bool isBuildingInteractable = closestInteractable is BuildingInteractableBase;
                    if (isGatherObject)
                    {
                        msg = "채집";
                        player.Animator.SetBool(gathering, true);
                    }
                    else if (isBuildingInteractable) msg = "상호작용";
                    else msg = "작업";
                    toastMessageUI.Show($"{msg} 중....", holdTime);

                    while (elapsed < holdTime)
                    {
                        elapsed += Time.deltaTime;
                        float progress = elapsed / holdTime;
                        holdUI.UpdateProgress(progress);

                        await UniTask.Yield(cancellationToken: cts.Token);
                    }

                    toastMessageUI.Show($"{msg} 완료!", 2f);
                    holdUI.Hide();
                }
                closestInteractable.Interact(player);
                player.Animator.SetBool(gathering, false);
                player.isInteracting = false;
            }
        }
        catch (System.OperationCanceledException)
        {
            toastMessageUI.Show($"{msg} 중단!", 2f);
            holdUI.Hide(); // 취소 시에도 UI 숨기기
            player.Animator.SetBool(gathering, false);
            player.isInteracting = false;
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
            // if (hit.GetComponent<GatherObject>() != null)
            //     continue;

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
            if (currentHighlight != null)
            {
                if (currentHighlight.TryGetComponent<GatherObject>(out _))
                    currentHighlight.HidePrompt();
                else
                    currentHighlight.Hide();
            }

            currentHighlight = closestHighlight;

            if (currentHighlight != null)
            {
                if (currentHighlight.TryGetComponent<GatherObject>(out _))
                    currentHighlight.ShowPrompt();
                else
                    currentHighlight.Show(); 
            }
        }
    }
}
