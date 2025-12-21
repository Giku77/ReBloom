using Cysharp.Threading.Tasks;
using System;
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
    private PlayerAnimation anim;
    private InteractionHighlight hilight;

    private bool isPlayingPickupAnim = false;

    private string savePrompt = string.Empty;

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
            if (!col.TryGetComponent<IInteractable>(out var candidate)) continue;

            Vector3 closestPoint = col.ClosestPoint(transform.position);
            float dist = Vector3.Distance(transform.position, closestPoint);

            Vector3 dir = (closestPoint - transform.position).normalized;
            if (Physics.Raycast(transform.position + Vector3.up * 1f, dir, out RaycastHit hit, dist, ~0))
            {
                if (hit.collider != col)
                {
                    continue;
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


    private void CheckForInteractable()
    {
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
            if (currentHighlight.TryGetComponent<GatherObject>(out _) ||
                currentHighlight.TryGetComponent<WorldItem>(out _))
                currentHighlight.ShowPrompt();
            else
                currentHighlight.Show();

            if (currentHighlight.TryGetComponent<OutlineToggle>(out var outline))
                outline.SetOutlined(true);
        }
    }

    private void ClearHighlight()
    {
        if (currentHighlight == null)
            return;

        if (currentHighlight.TryGetComponent<GatherObject>(out _) ||
            currentHighlight.TryGetComponent<WorldItem>(out _))
            currentHighlight.HidePrompt();
        else
            currentHighlight.Hide();

        if (currentHighlight.TryGetComponent<OutlineToggle>(out var outline))
            outline.SetOutlined(false);

        currentHighlight = null;
    }

    private void Update()
    {
        //if (player.WasJumping || player.JumpRequested)
        //{
        //    CancelInteract();
        //    ClearHighlight();
        //    return;
        //}

        //CheckForInteractable();

        if (player.WasJumping || player.JumpRequested)
        {
            CancelInteract();
            ClearHighlight();
            return;
        }

        if (!TryGetInteractable(out _, out InteractionHighlight newHighlight, out _))
        {
            CancelInteract();
            ClearHighlight();
            return;
        }

        if (currentHighlight != newHighlight)
        {
            ClearHighlight();
            currentHighlight = newHighlight;
            currentHighlight.Show();
        }
    }

    //private async UniTask StartInteract()
    //{
    //    CancelInteract();
    //    cts = new CancellationTokenSource();
    //    var msg = string.Empty;
    //    try
    //    {
    //        Vector3 bottom = transform.position + Vector3.up * 0.5f;
    //        Vector3 top = transform.position + Vector3.up * 1.3f;
    //        Collider[] hits = Physics.OverlapCapsule(bottom, top, interactRange, interactLayer);

    //        //Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
    //        IInteractable closestInteractable = null;
    //        float closestDistance = float.MaxValue;

    //        foreach (var hit in hits)
    //        {
    //            if (hit.TryGetComponent<IInteractable>(out var interactable))
    //            {
    //                Vector3 toTarget = hit.transform.position - transform.position;
    //                float distance = toTarget.magnitude;
    //                float dot = Vector3.Dot(transform.forward, toTarget.normalized);

    //                bool isInRange = (distance < 0.5f) || (dot > 0.3f && distance < closestDistance);

    //                if (isInRange && distance < closestDistance)
    //                {
    //                    closestDistance = distance;
    //                    closestInteractable = interactable;
    //                    hilight = hit.GetComponent<InteractionHighlight>();
    //                }
    //            }
    //        }

    //        if (closestInteractable != null && closestInteractable.CanInteract())
    //        {
    //            float holdTime = closestInteractable.HoldTime;

    //            bool isWorldItem = closestInteractable is WorldItem;
    //            if (isWorldItem)
    //            {
    //                //anim.PlayPickUp();
    //                //player.isInteracting = true;
    //                //await UniTask.Delay(800);

    //                if (!isPlayingPickupAnim)
    //                {
    //                    isPlayingPickupAnim = true;
    //                    anim.PlayPickUp();
    //                    player.isInteracting = true;
    //                    await UniTask.Delay(800);
    //                    player.isInteracting = false;
    //                    isPlayingPickupAnim = false;
    //                    SoundManager.I?.PlayGetWorldItem();
    //                }
    //                else
    //                {
    //                    await UniTask.Delay(100);
    //                }
    //            }
                
    //            saveprompt = hilight.promptFormat;
    //            if (holdTime > 0f)
    //            {
    //                player.isInteracting = true;
    //                float elapsed = 0f;
    //                hilight.HoldPromptUI?.Show();
    //                bool isGatherObject = closestInteractable is GatherObject;
    //                bool isBuildingInteractable = closestInteractable is BuildingInteractableBase;
    //                bool isWaterSource = closestInteractable is WaterSource;
    //                bool isDeathBox = closestInteractable is WorldDeathBox;
    //                if (isGatherObject)
    //                {
    //                    msg = "조사";
    //                    anim.SetGathering(true);
    //                    SoundManager.I?.PlayGather(toolType);
    //                }
    //                else if (isBuildingInteractable) msg = "상호작용";
    //                else if (isDeathBox)
    //                {
    //                    msg = "회수";
    //                    anim.PlayPickUp();
    //                }
    //                else if (isWaterSource)
    //                {
    //                    msg = "물 뜨는";
    //                    anim.PlayPickUp();
    //                }
    //                else msg = "작업";
    //                if (hilight)
    //                {
    //                    hilight.promptFormat = $"{msg} 중...";
    //                    hilight.ShowPrompt();
    //                }
    //                else ToastMessageUI.Instance.Show($"{msg} 중....", holdTime);

    //                while (elapsed < holdTime)
    //                {
    //                    elapsed += Time.deltaTime;
    //                    float progress = elapsed / holdTime;
    //                    hilight.HoldPromptUI?.UpdateProgress(progress);

    //                    await UniTask.Yield(cancellationToken: cts.Token);
    //                }

    //                if (hilight)
    //                {
    //                    //hilight.promptFormat = $"{msg} 완료!";
    //                    //hilight.ShowPrompt();
    //                    //hilight.HidePrompt();
    //                    hilight.promptFormat = saveprompt;
    //                    hilight.ShowPrompt();
    //                }
    //                else ToastMessageUI.Instance.Show($"{msg} 완료!", 2f);
    //                hilight.HoldPromptUI?.Hide();
    //            }
    //            closestInteractable.Interact(player);
    //            anim.SetGathering(false);
    //            SoundManager.I?.StopGather();
    //            player.isInteracting = false;
    //        }
    //    }
    //    catch (System.OperationCanceledException)
    //    {
    //        //toastMessageUI.Show($"{msg} 중단!", 2f);
    //        if (hilight)
    //        {
    //            hilight.promptFormat = saveprompt;
    //            hilight.ShowPrompt();
    //        }
    //        hilight.HoldPromptUI?.Hide(); // 취소 시에도 UI 숨기기
    //        anim.SetGathering(false);
    //        player.isInteracting = false;
    //        SoundManager.I?.StopGather();
    //        CancelInteract();
    //    }
    //}

    //private void CheckForInteractable()
    //{
    //    Vector3 bottom = transform.position + Vector3.up * 0.5f;
    //    Vector3 top = transform.position + Vector3.up * 1.5f;
    //    Collider[] hits = Physics.OverlapCapsule(bottom, top, interactRange, interactLayer);

    //    //Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
    //    InteractionHighlight closestHighlight = null;
    //    float closestDistance = float.MaxValue;

    //    foreach (var hit in hits)
    //    {
    //        // if (hit.GetComponent<GatherObject>() != null)
    //        //     continue;

    //        if (hit.TryGetComponent<InteractionHighlight>(out var highlight))
    //        {
    //            Vector3 toTarget = hit.transform.position - transform.position;
    //            float distance = toTarget.magnitude;
    //            float dot = Vector3.Dot(transform.forward, toTarget.normalized);

    //            bool isInRange = (distance < 0.5f) || (dot > 0.3f && distance < closestDistance);

    //            if (isInRange && distance < closestDistance)
    //            {
    //                closestDistance = distance;
    //                closestHighlight = highlight;
    //            }
    //        }
    //    }

    //    if (currentHighlight != closestHighlight)
    //    {
    //        if (currentHighlight != null)
    //        {
    //            if (currentHighlight.TryGetComponent<GatherObject>(out _) || currentHighlight.TryGetComponent<WorldItem>(out _))

    //                currentHighlight.HidePrompt();
    //            else
    //                currentHighlight.Hide();
                
    //            if (currentHighlight.TryGetComponent<OutlineToggle>(out var outlineToggle))
    //            {
    //                outlineToggle.SetOutlined(false);
    //            }
    //        }

    //        currentHighlight = closestHighlight;

    //        if (currentHighlight != null)
    //        {
    //            if (currentHighlight.TryGetComponent<GatherObject>(out _) || currentHighlight.TryGetComponent<WorldItem>(out _))
    //                currentHighlight.ShowPrompt();
    //            else
    //                currentHighlight.Show(); 
    //            if (currentHighlight.TryGetComponent<OutlineToggle>(out var outlineToggle))
    //            {
    //                outlineToggle.SetOutlined(true);
    //            }
    //        }
    //    }
    //}

    private void ToolTypeChange(int value)
    { 
        toolType = value;
    }

    private async UniTask StartInteract()
    {
        if (player.WasJumping || player.JumpRequested)
            return;

        CancelInteract();
        cts = new CancellationTokenSource();

        try
        {
            if (!TryGetInteractable(out IInteractable interactable, out hilight, out _))
                return;

            if (!interactable.CanInteract())
                return;

            float holdTime = interactable.HoldTime;

            if (interactable is WorldItem)
            {
                if (!isPlayingPickupAnim)
                {
                    isPlayingPickupAnim = true;
                    anim.PlayPickUp();
                    player.isInteracting = true;
                    await UniTask.Delay(800);
                    player.isInteracting = false;
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

                string msg = GetInteractMessage(interactable);
                savePrompt = hilight ? hilight.promptFormat : string.Empty;

                if (hilight)
                {
                    hilight.promptFormat = $"{msg} 중...";
                    hilight.ShowPrompt();
                }

                while (elapsed < holdTime)
                {
                    elapsed += Time.deltaTime;
                    hilight?.HoldPromptUI?.UpdateProgress(elapsed / holdTime);
                    await UniTask.Yield(cancellationToken: cts.Token);
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
        }
        finally
        {
            anim.SetGathering(false);
            player.isInteracting = false;
            SoundManager.I?.StopGather();
            CancelInteract();
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
