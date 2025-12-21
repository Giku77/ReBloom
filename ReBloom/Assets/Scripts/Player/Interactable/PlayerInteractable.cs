using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private LayerMask interactLayer;

    private PlayerController player;
    private PlayerAnimation anim;

    private CancellationTokenSource cts;
    private InteractionHighlight currentHighlight;
    private InteractionHighlight hilight;

    private bool isPlayingPickupAnim;
    private string savePrompt;

    public int toolType = 0;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        anim = GetComponent<PlayerAnimation>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
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

    private bool TryGetInteractable(
        out IInteractable interactable,
        out InteractionHighlight highlight,
        out Collider hitCollider)
    {
        interactable = null;
        highlight = null;
        hitCollider = null;

        Vector3 bottom = transform.position + Vector3.up * 0.5f;
        Vector3 top = transform.position + Vector3.up * 1.5f;

        Collider[] hits = Physics.OverlapCapsule(bottom, top, 0.6f, interactLayer);
        if (hits.Length == 0) return false;

        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            if (!col.TryGetComponent<IInteractable>(out var candidate))
                continue;

            if (!candidate.CanInteract())
                continue;

            Vector3 point = col.ClosestPoint(transform.position);
            float dist = Vector3.Distance(transform.position, point);

            if (dist < closestDist)
            {
                closestDist = dist;
                interactable = candidate;
                highlight = col.GetComponent<InteractionHighlight>();
                hitCollider = col;
            }
        }

        return interactable != null;
    }

    private void Update()
    {
        if (player.WasJumping || player.JumpRequested)
        {
            CancelInteract();
            ClearHighlight();
            return;
        }

        if (!TryGetInteractable(out _, out InteractionHighlight newHighlight, out _))
        {
            ClearHighlight();
            return;
        }

        if (currentHighlight == newHighlight)
            return;

        ClearHighlight();
        currentHighlight = newHighlight;

        if (currentHighlight != null)
        {
            currentHighlight.ShowPrompt();

            if (currentHighlight.TryGetComponent<OutlineToggle>(out var outline))
                outline.SetOutlined(true);
        }
    }

    private void ClearHighlight()
    {
        if (currentHighlight == null)
            return;

        currentHighlight.HidePrompt();

        if (currentHighlight.TryGetComponent<OutlineToggle>(out var outline))
            outline.SetOutlined(false);

        currentHighlight = null;
    }

    private async UniTask StartInteract()
    {
        CancelInteract();
        cts = new CancellationTokenSource();

        try
        {
            if (!TryGetInteractable(out IInteractable interactable, out hilight, out _))
                return;

            float holdTime = interactable.HoldTime;

            // 월드 아이템 (즉시 습득)
            if (interactable is WorldItem)
            {
                if (!isPlayingPickupAnim)
                {
                    isPlayingPickupAnim = true;
                    anim.PlayPickUp();
                    await UniTask.Delay(800);
                    isPlayingPickupAnim = false;
                    SoundManager.I?.PlayGetWorldItem();
                }

                interactable.Interact(player);
                return;
            }

            if (holdTime > 0f)
            {
                player.isInteracting = true;
                float elapsed = 0f;

                hilight?.HoldPromptUI?.Show();
                savePrompt = hilight ? hilight.promptFormat : string.Empty;

                if (hilight)
                {
                    hilight.promptFormat = $"{GetInteractMessage(interactable)} 중...";
                    hilight.ShowPrompt();
                }

                while (elapsed < holdTime)
                {
                    elapsed += Time.deltaTime;
                    hilight?.HoldPromptUI?.UpdateProgress(elapsed / holdTime);
                    await UniTask.Yield(cts.Token);
                }

                if (hilight)
                {
                    hilight.promptFormat = savePrompt;
                    hilight.ShowPrompt();
                    hilight.HoldPromptUI?.Hide();
                }
            }

            interactable.Interact(player);
        }
        catch (OperationCanceledException) { }
        finally
        {
            anim.SetGathering(false);
            player.isInteracting = false;
            SoundManager.I?.StopGather();
            CancelInteract();
            ClearHighlight();
        }
    }

    private string GetInteractMessage(IInteractable interactable)
    {
        if (interactable is GatherObject)
        {
            anim.SetGathering(true);
            SoundManager.I?.PlayGather(toolType);
            return "조사";
        }
        if (interactable is BuildingInteractableBase) return "상호작용";
        if (interactable is WorldDeathBox) return "회수";
        if (interactable is WaterSource) return "물 뜨는";
        return "작업";
    }
}
