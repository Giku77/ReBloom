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
    private PlayerAnimation anim;
    private InteractionHighlight hilight;

    private bool isPlayingPickupAnim = false;

    private string saveprompt = string.Empty;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        anim = GetComponent<PlayerAnimation>();
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
            Vector3 bottom = transform.position + Vector3.up * 0.5f;
            Vector3 top = transform.position + Vector3.up * 1.3f;
            Collider[] hits = Physics.OverlapCapsule(bottom, top, interactRange, interactLayer);

            //Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IInteractable>(out var interactable))
                {
                    Vector3 toTarget = hit.transform.position - transform.position;
                    float distance = toTarget.magnitude;
                    float dot = Vector3.Dot(transform.forward, toTarget.normalized);

                    bool isInRange = (distance < 0.5f) || (dot > 0.3f && distance < closestDistance);

                    if (isInRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                        hilight = hit.GetComponent<InteractionHighlight>();
                    }
                }
            }

            if (closestInteractable != null && closestInteractable.CanInteract())
            {
                float holdTime = closestInteractable.HoldTime;

                bool isWorldItem = closestInteractable is WorldItem;
                if (isWorldItem)
                {
                    //anim.PlayPickUp();
                    //player.isInteracting = true;
                    //await UniTask.Delay(800);

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
                    else
                    {
                        await UniTask.Delay(100);
                    }
                }
                
                saveprompt = hilight.promptFormat;
                if (holdTime > 0f)
                {
                    player.isInteracting = true;
                    float elapsed = 0f;
                    hilight.HoldPromptUI?.Show();
                    bool isGatherObject = closestInteractable is GatherObject;
                    bool isBuildingInteractable = closestInteractable is BuildingInteractableBase;
                    bool isWaterSource = closestInteractable is WaterSource;
                    bool isDeathBox = closestInteractable is WorldDeathBox;
                    if (isGatherObject)
                    {
                        msg = "조사";
                        anim.SetGathering(true);
                    }
                    else if (isBuildingInteractable) msg = "상호작용";
                    else if (isDeathBox)
                    {
                        msg = "회수";
                        anim.PlayPickUp();
                    }
                    else if (isWaterSource)
                    {
                        msg = "물 뜨는";
                        anim.PlayPickUp();
                    }
                    else msg = "작업";
                    if (hilight)
                    {
                        hilight.promptFormat = $"{msg} 중...";
                        hilight.ShowPrompt();
                    }
                    else ToastMessageUI.Instance.Show($"{msg} 중....", holdTime);

                    while (elapsed < holdTime)
                    {
                        elapsed += Time.deltaTime;
                        float progress = elapsed / holdTime;
                        hilight.HoldPromptUI?.UpdateProgress(progress);

                        await UniTask.Yield(cancellationToken: cts.Token);
                    }

                    if (hilight)
                    {
                        //hilight.promptFormat = $"{msg} 완료!";
                        //hilight.ShowPrompt();
                        //hilight.HidePrompt();
                        hilight.promptFormat = saveprompt;
                        hilight.ShowPrompt();
                    }
                    else ToastMessageUI.Instance.Show($"{msg} 완료!", 2f);
                    hilight.HoldPromptUI?.Hide();
                }
                closestInteractable.Interact(player);
                anim.SetGathering(false);

                player.isInteracting = false;
            }
        }
        catch (System.OperationCanceledException)
        {
            //toastMessageUI.Show($"{msg} 중단!", 2f);
            if (hilight)
            {
                hilight.promptFormat = saveprompt;
                hilight.ShowPrompt();
            }
            hilight.HoldPromptUI?.Hide(); // 취소 시에도 UI 숨기기
            anim.SetGathering(false);
            player.isInteracting = false;
            CancelInteract();
        }
    }

    private void CheckForInteractable()
    {
        Vector3 bottom = transform.position + Vector3.up * 0.5f;
        Vector3 top = transform.position + Vector3.up * 1.5f;
        Collider[] hits = Physics.OverlapCapsule(bottom, top, interactRange, interactLayer);

        //Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
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

                bool isInRange = (distance < 0.5f) || (dot > 0.3f && distance < closestDistance);

                if (isInRange && distance < closestDistance)
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
                if (currentHighlight.TryGetComponent<GatherObject>(out _) || currentHighlight.TryGetComponent<WorldItem>(out _))

                    currentHighlight.HidePrompt();
                else
                    currentHighlight.Hide();
                
                if (currentHighlight.TryGetComponent<OutlineToggle>(out var outlineToggle))
                {
                    outlineToggle.SetOutlined(false);
                }
            }

            currentHighlight = closestHighlight;

            if (currentHighlight != null)
            {
                if (currentHighlight.TryGetComponent<GatherObject>(out _) || currentHighlight.TryGetComponent<WorldItem>(out _))
                    currentHighlight.ShowPrompt();
                else
                    currentHighlight.Show(); 
                if (currentHighlight.TryGetComponent<OutlineToggle>(out var outlineToggle))
                {
                    outlineToggle.SetOutlined(true);
                }
            }
        }
    }
}
