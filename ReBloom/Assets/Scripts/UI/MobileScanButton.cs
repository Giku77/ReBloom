using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모바일 스캔 버튼 + 쿨타임 슬라이더
/// </summary>
public class MobileScanButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScanController scanController;
    [SerializeField] private Button scanButton;
    [SerializeField] private Slider cooldownSlider;

    [Header("Optional")]
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Start()
    {
        if (scanButton != null)
        {
            scanButton.onClick.AddListener(OnScanButtonClicked);
        }

        // 시작할 때 슬라이더 꽉 참 (사용 가능 상태)
        if (cooldownSlider != null)
        {
            cooldownSlider.interactable = false;  // 드래그 방지
            cooldownSlider.value = 1f;
        }
    }

    private void Update()
    {
        UpdateCooldownUI();
    }

    private void OnScanButtonClicked()
    {
        if (scanController == null)
        {
            Debug.LogError("[MobileScanButton] scanController가 null!");
            return;
        }

        scanController.TriggerScan();
    }

    private void UpdateCooldownUI()
    {
        if (scanController == null)
        {
            Debug.LogWarning("[MobileScanButton] scanController가 null!");
            return;
        }

        float fill = scanController.CooldownFillAmount;

        if (cooldownSlider != null)
        {
            cooldownSlider.value = fill;
        }
        else
        {
            Debug.LogWarning("[MobileScanButton] cooldownSlider가 null!");
        }

        if (scanController == null) return;

        if (cooldownSlider != null)
        {
            cooldownSlider.value = scanController.CooldownFillAmount;
        }

        if (buttonIcon != null)
        {
            buttonIcon.color = scanController.IsOnCooldown ? cooldownColor : normalColor;
        }

        if (scanButton != null)
        {
            scanButton.interactable = !scanController.IsOnCooldown;
        }
    }

    private void OnDestroy()
    {
        if (scanButton != null)
        {
            scanButton.onClick.RemoveListener(OnScanButtonClicked);
        }
    }
}