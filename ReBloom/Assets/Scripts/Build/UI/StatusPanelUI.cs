using TMPro;
using UnityEngine;

public class StatusPanelUI : MonoBehaviour
{
    [Header("Value Texts (TMP)")]
    [SerializeField] private TextMeshProUGUI energyValueText;
    [SerializeField] private TextMeshProUGUI researchValueText;
    [SerializeField] private TextMeshProUGUI greeningValueText;

    [Header("Format Options")]
    [SerializeField] private bool padTwoDigits = true;   // 00 형태로
    [SerializeField] private bool roundToInt = true;     // 표시를 정수로

    private ResearchManager rm;

    private void Awake()
    {
        rm = ResearchManager.I;
    }

    private void OnEnable()
    {
        if (rm == null) rm = ResearchManager.I;
        if (rm == null) return;

        rm.OnEnergyChanged += HandleEnergyChanged;
        rm.OnProgressChanged += HandleProgressChanged;
        rm.OnGreeningChanged += HandleGreeningChanged;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (rm == null) return;

        rm.OnEnergyChanged -= HandleEnergyChanged;
        rm.OnProgressChanged -= HandleProgressChanged;
        rm.OnGreeningChanged -= HandleGreeningChanged;
    }

    public void RefreshAll()
    {
        if (rm == null) return;

        HandleEnergyChanged(rm.CurrentEnergy);
        HandleProgressChanged(rm.CurrentProgress);
        HandleGreeningChanged(rm.CurrentGreening);
    }

    // ----------------- handlers -----------------

    private void HandleEnergyChanged(float value)
    {
        if (energyValueText == null) return;

        int v = roundToInt ? Mathf.RoundToInt(value) : (int)value;
        energyValueText.text = $"{FormatNumber(v)}kw";
    }

    private void HandleProgressChanged(float value)
    {
        if (researchValueText == null) return;

        int v = roundToInt ? Mathf.RoundToInt(value) : (int)value;
        researchValueText.text = FormatNumber(v);
    }

    private void HandleGreeningChanged(float value)
    {
        if (greeningValueText == null) return;

        int v = roundToInt ? Mathf.RoundToInt(value) : (int)value;
        greeningValueText.text = $"{FormatNumber(v)}%";
    }

    private string FormatNumber(int v)
    {
        if (!padTwoDigits) return v.ToString();
        return v < 100 ? v.ToString("00") : v.ToString();
    }
}
