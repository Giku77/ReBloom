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

    private void OnEnable()
    {
        PlayerEquipManager.OnToolTypeChange += ToolTypeChange;
    }

    private void OnDisable()
    {
        PlayerEquipManager.OnToolTypeChange -= ToolTypeChange;
    }

    private void Update()
    {
        if (player.isDead || player.WasJumping || player.JumpRequested)
        {
            CancelInteract();
            ClearHighlight();
            return;
        }

        if (!TryGetInteractable(out IInteractable interactable, out InteractionHighlight newHighlight, out _))
        {
            CancelInteract();
            ClearHighlight();
            return;
        }

        if (currentHighlight == newHighlight)
        {
            if (player.isInteracting)
                return;

            if (interactable is GatherObject gather)
            {
                currentHighlight.ShowPrompt(gather.GetCurrentPromptText());
            }
            return;
        }

        ClearHighlight();
        currentHighlight = newHighlight;

        if (currentHighlight != null)
        {
            // 프롬프트만 표시 (불빛 X)
            currentHighlight.ShowPrompt();

            // 외곽선만 켜기
            if (currentHighlight.TryGetComponent<OutlineToggle>(out var outline))
                outline.SetOutlined(true);
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started)
            StartInteract().Forget();
        else if (context.canceled && player.isInteracting)
            CancelInteract();
    }

    private void CancelInteract()
    {
        if (hilight != null)
        {
            hilight.HoldPromptUI?.Hide();
            hilight.HidePrompt();
        }

        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private bool TryGetInteractable(out IInteractable interactable, out InteractionHighlight highlight, out Collider hitCollider)
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

            Vector3 closestPoint = col.ClosestPoint(transform.position);
            float dist = Vector3.Distance(transform.position, closestPoint);

            // 벽 너머 오브젝트 체크
            Vector3 dir = (closestPoint - transform.position).normalized;
            if (Physics.Raycast(transform.position + Vector3.up * 1f, dir, out RaycastHit hit, dist, ~0))
            {
                if (hit.collider != col)
                {
                    continue; // 중간에 벽이 있으면 스킵
                }
            }

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

    private void ClearHighlight()
    {
        if (currentHighlight == null)
            return;

        currentHighlight.HidePrompt();

        if (currentHighlight.TryGetComponent<OutlineToggle>(out var outline))
            outline.SetOutlined(false);

        currentHighlight = null;
    }

    private void ToolTypeChange(int value)
    {
        toolType = value;
    }

    private async UniTask StartInteract()
    {
        if (player.isDead || player.WasJumping || player.JumpRequested)
            return;

        if (!TryGetInteractable(out IInteractable interactable, out hilight, out _))
            return;

        if (!interactable.CanInteract())
        {
            if (interactable is GatherObject gather)
            {
                string msg = gather.GetCannotInteractMessage();
                if (!string.IsNullOrEmpty(msg))
                {
                    ToastMessageUI.Instance?.Show(msg);
                }
            }
            else
            {
                ToastMessageUI.Instance?.Show("아직 재생성 중입니다.");
            }
            return;
        }

        CancelInteract();
        cts = new CancellationTokenSource();

        try
        {
            float holdTime = interactable.HoldTime;

            // 월드 아이템 (즉시 습득)
            if (interactable is WorldItem)
            {
                if (!isPlayingPickupAnim)
                {
                    isPlayingPickupAnim = true;
                    player.SetBlocked(true);
                    player.isInteracting = true;
                    anim.PlayPickUp();
                    await UniTask.Delay(800);
                    player.isInteracting = false;
                    player.SetBlocked(false);

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

                string msg = GetInteractMessage(interactable);

                if (hilight)
                {
                    hilight.promptFormat = $"{msg} 중...";
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
        catch (OperationCanceledException)
        {
            if (hilight)
            {
                hilight.promptFormat = savePrompt;
                hilight.ShowPrompt();
                hilight.HoldPromptUI?.Hide();
            }

            if (isPlayingPickupAnim)
            {
                player.SetBlocked(false);
                isPlayingPickupAnim = false;
            }
        }
        finally
        {
            anim.SetGathering(false);
            player.isInteracting = false;
            //SoundManager.I?.StopGather();
            //CancelInteract();
        }
    }

    private string GetInteractMessage(IInteractable interactable)
    {
        if (interactable is GatherObject)
        {
            anim.SetGathering(true);
            //SoundManager.I?.PlayGather(toolType);
            return "조사";
        }
        if (interactable is BuildingInteractableBase) return "상호작용";
        if (interactable is WorldDeathBox) return "회수";
        if (interactable is WaterSource) return "물 뜨는";
        return "작업";
    }

    public void TriggerInteract()
    {
        StartInteract().Forget();
    }

    public void CancelMobileInteract()
    {
        CancelInteract();
    }
}