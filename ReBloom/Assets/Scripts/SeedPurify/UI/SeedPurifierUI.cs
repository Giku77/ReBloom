using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeedPurifierUI : UIBase
{
    [Header("UI")]
    [SerializeField] private Button btnInsertUnidentified;
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnCancel;
    [SerializeField] private Button btnTake;
    [SerializeField] private Image progressSlider;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Slot Labels (optional)")]
    [SerializeField] private TextMeshProUGUI inputText;
    [SerializeField] private Image inputIcon;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private Image outputIcon;

    [Header("Insert Setting")]
    [SerializeField] private int[] unidentifiedSeedItemIds = { 4101001, 4101002, 4101003 };

    private SeedPurifierMachine machine;
    private PlayerController player;

    public void Bind(SeedPurifierMachine machine, PlayerController player)
    {
        // 이전 머신 이벤트 해제
        if (this.machine != null)
        {
            this.machine.OnChanged -= Refresh;
            this.machine.OnProgress -= OnProgress;
        }

        this.machine = machine;
        this.player = player;

        // 새 머신 이벤트 구독
        if (this.machine != null)
        {
            this.machine.OnChanged += Refresh;
            this.machine.OnProgress += OnProgress;
        }

        HookButtonsOnce();
        Refresh();
        OnProgress(0f);
    }

    private bool hooked;

    private bool TryConsumeAnyUnidentifiedSeed(PlayerController player, out int usedItemId)
    {
        usedItemId = 0;

        var inv = player.Inventory;
        if (inv == null) return false;

        foreach (var id in unidentifiedSeedItemIds)
        {
            if (inv.HasItem(id, 1))
            {
                inv.RemoveItem(id, 1);
                usedItemId = id;
                return true;
            }
        }
        return false;
    }

    private void HookButtonsOnce()
    {
        if (hooked) return;
        hooked = true;

        btnInsertUnidentified?.onClick.AddListener(() =>
        {
            if (machine == null || player == null) return;

            // 이미 input 있으면 막기
            if (machine.PeekInputItemId() != 0)
            {
                SoundManager.I?.PlayError();
                ToastMessageUI.Instance?.Show("이미 미확인 종자가 들어있습니다.");
                return;
            }

            if (TryConsumeAnyUnidentifiedSeed(player, out int usedItemId))
            {
                SoundManager.I?.PlaySeed();
                machine.SetInput(usedItemId, 1); 
                Refresh();
            }
            else
            {
                SoundManager.I?.PlayError();
                ToastMessageUI.Instance?.Show("미확인 종자가 없습니다.");
            }
        });

        btnStart?.onClick.AddListener(() =>
        {
            if (machine == null) return;
            machine.StartProcess();
            Refresh();
        });

        btnCancel?.onClick.AddListener(() =>
        {
            if (machine == null) return;
            machine.CancelProcess();
            Refresh();
        });

        btnTake?.onClick.AddListener(() =>
        {
            if (machine == null) return;
            machine.TakeOutput();
            Refresh();
        });
    }

    private void OnDisable()
    {
        if (machine != null)
        {
            machine.OnChanged -= Refresh;
            machine.OnProgress -= OnProgress;
        }
    }

    private void OnProgress(float t)
    {
        if (progressSlider != null) progressSlider.fillAmount = t;

        if (timeText != null)
        {
            if (machine != null && machine.CurrentState == SeedPurifierMachine.State.Processing)
            {
                float remain = Mathf.Lerp(5f, 0f, t);
                timeText.text = $"{Mathf.CeilToInt(remain)}초";
            }
            else timeText.text = "";
        }
    }

    private void Refresh()
    {
        if (machine == null) return;

        var st = machine.CurrentState;
        bool hasInput = machine.PeekInputItemId() != 0;
        bool hasOutput = machine.PeekOutputItemId() != 0;

        if (btnInsertUnidentified != null)
            btnInsertUnidentified.interactable = (st == SeedPurifierMachine.State.Idle && !hasInput);

        if (btnStart != null)
            btnStart.interactable = machine.CanStart();

        if (btnCancel != null)
            btnCancel.interactable = (st == SeedPurifierMachine.State.Processing);

        if (btnTake != null)
            btnTake.interactable = (st == SeedPurifierMachine.State.OutputReady && hasOutput);

        if (inputText != null)
        {
            var itemdata = hasInput ? ItemDatabase.I.GetItem(machine.PeekInputItemId()) : null;
            inputText.text = hasInput ? $"{itemdata.itemName}" : "";
            if (inputIcon != null)
            {
                if (hasInput)
                {
                    inputIcon.sprite = itemdata.icon;
                    inputIcon.gameObject.SetActive(true);
                }
                else
                    inputIcon.gameObject.SetActive(false);
            }
        }
            
        if (outputText != null)
        {
            var itemdata = hasOutput ? ItemDatabase.I.GetItem(machine.PeekOutputItemId()) : null;
            outputText.text = hasOutput ? $"{itemdata.itemName}" : "";
            if (outputIcon != null)
            {
                if (hasOutput)
                {
                    outputIcon.sprite = itemdata.icon;
                    outputIcon.gameObject.SetActive(true);
                }
                else
                    outputIcon.gameObject.SetActive(false);
            }
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        SoundManager.I?.PlayOpenBox();
    }

    protected override void OnHide()
    {
        base.OnHide();
        SoundManager.I?.PlayCloseCraftingTable();
    }
}
