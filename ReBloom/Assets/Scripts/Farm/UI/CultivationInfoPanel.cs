using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CultivationInfoPanel : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;      // 예: "배양 슬롯"
    [SerializeField] private TextMeshProUGUI stateText;      // "가동 중 / 수거 가능 / 빈 칸"
    [SerializeField] private TextMeshProUGUI remainText;     // "남은 시간: 01:23"
    [SerializeField] private TextMeshProUGUI powerText;      // "전력: 25kW / 필요: 30kW (부족)"

    [Header("Output")]
    [SerializeField] private Image outputIcon;
    [SerializeField] private TextMeshProUGUI outputNameText;
    [SerializeField] private TextMeshProUGUI outputCountText;

    [Header("Buttons")]
    [SerializeField] private Button collectBtn;
    [SerializeField] private Button closeBtn;

    private CultivationMachine currentMachine;

    public event Action OnCollectClicked;

    private float _nextRefreshTime;

    private void Awake()
    {
        if (collectBtn != null)
            collectBtn.onClick.AddListener(() => OnCollectClicked?.Invoke());

        if (closeBtn != null)
            closeBtn.onClick.AddListener(Hide);

        Hide();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            currentMachine?.DebugForceReadyToCollect();
        }

        if (Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + 0.5f;
            if (currentMachine != null) Refresh(currentMachine);
        }
    }

    public void Show(CultivationMachine machine)
    {
        currentMachine = machine;
        Refresh(machine);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        currentMachine = null;
        gameObject.SetActive(false);
    }

    public void Refresh(CultivationMachine machine)
    {
        if (machine == null) return;
        currentMachine = machine;

        var slot = machine.Slot;

        if (titleText)
        {
            string title = "배양 슬롯";

            if (slot != null && slot.state != CultivationSlotState.Empty)
            {
                var seedItem = ItemDatabase.I.GetItem(slot.seedItemId);
                if (seedItem != null)
                    title = seedItem.itemName;
            }

            titleText.text = title;
        }

        if (slot == null || slot.state == CultivationSlotState.Empty)
        {
            if (stateText) stateText.text = "빈 칸";
            if (remainText) remainText.text = "남은 시간: -";
            SetOutputUI(0, 0);
            SetPowerUI(slot != null ? slot.requiredPowerKw : 0f);
            if (collectBtn) collectBtn.interactable = false;
            return;
        }

        if (stateText)
        {
            stateText.text = slot.state switch
            {
                CultivationSlotState.Running => "가동 중",
                CultivationSlotState.ReadyToCollect => "수거 가능",
                _ => "알 수 없음"
            };
        }

        if (remainText)
        {
            remainText.text = slot.state == CultivationSlotState.Running
                ? $"남은 시간: {FormatTime(slot.remainTime)}"
                : "남은 시간: 완료";
        }

        SetOutputUI(slot.outputItemId, slot.outputCount);
        SetPowerUI(slot.requiredPowerKw);

        if (collectBtn)
            collectBtn.interactable = (slot.state == CultivationSlotState.ReadyToCollect);
    }

    private void SetOutputUI(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0)
        {
            if (outputIcon) outputIcon.enabled = false;
            if (outputNameText) outputNameText.text = "-";
            if (outputCountText) outputCountText.text = "";
            return;
        }

        var item = ItemDatabase.I.GetItem(itemId);
        if (item == null)
        {
            if (outputIcon) outputIcon.enabled = false;
            if (outputNameText) outputNameText.text = $"Item {itemId}";
            if (outputCountText) outputCountText.text = $"x{count}";
            return;
        }

        if (outputIcon)
        {
            // outputIcon.sprite = item.icon;
            outputIcon.enabled = true;
        }

        if (outputNameText) outputNameText.text = item.itemName;
        if (outputCountText) outputCountText.text = $"x{count}";
    }

    private void SetPowerUI(float requiredKw)
    {
        if (powerText == null) return;

        // TODO: ArcDB에서 현재 가용 전력 가져오기
        float availableKw = GetAvailablePowerKwMock();

        if (requiredKw <= 0f)
        {
            powerText.text = $"전력: {availableKw:0.#}kW";
            return;
        }

        bool enough = availableKw >= requiredKw;
        powerText.text = enough
            ? $"전력: {availableKw:0.#}kW / 필요: {requiredKw:0.#}kW"
            : $"전력: {availableKw:0.#}kW / 필요: {requiredKw:0.#}kW (부족)";
    }

    private float GetAvailablePowerKwMock() => 999f; // TODO 교체

    private static string FormatTime(float sec)
    {
        if (sec < 0f) sec = 0f;
        int total = Mathf.CeilToInt(sec);
        int m = total / 60;
        int s = total % 60;
        return $"{m:00}:{s:00}";
    }
}
