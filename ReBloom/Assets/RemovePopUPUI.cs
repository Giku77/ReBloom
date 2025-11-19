using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RemovePopUPUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private RemovePopUp controller;
    [SerializeField] private TextMeshProUGUI quantityTxt;
    [SerializeField] private TextMeshProUGUI contentTxt;
    [SerializeField] private Slider quantitySlider;

    [Header("Buttons")]
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button executeButton;
    [SerializeField] private Button cancelButton;

    #region 초기화
    private void Awake()
    {
        // 버튼 이벤트 연결
        increaseButton.onClick.AddListener(OnIncreaseButton);
        decreaseButton.onClick.AddListener(OnDecreaseButton);
        executeButton.onClick.AddListener(OnExecute);
        cancelButton.onClick.AddListener(OnCancel);

        // 슬라이더 이벤트 연결
        quantitySlider.onValueChanged.AddListener(OnSliderValueChanged);
    }
    /// <summary>
    /// 팝업 초기화
    /// </summary>
    public void Init(ItemBase item, int maxQuantity)
    {
        if (item == null)
        {
            Debug.LogError("[RemovePopUPUI] item이 null입니다!");
            return;
        }

        // 텍스트 설정
        contentTxt.text = $"'{item.itemName}'을(를)\n버리시겠습니까?";

        // 슬라이더 설정
        quantitySlider.minValue = 1;
        quantitySlider.maxValue = maxQuantity;
        quantitySlider.wholeNumbers = true;

        // 초기 수량 설정 (절반)
        int initialQuantity = Mathf.Max(1, maxQuantity / 2);
        controller.SetQuantity(initialQuantity);

        // 슬라이더도 동기화
        quantitySlider.value = initialQuantity;

        // UI 업데이트
        UpdateQuantityUI(initialQuantity);
    }
    #endregion

    #region 버튼 이벤트
    /// <summary>
    /// 실행 버튼 (아이템 제거)
    /// </summary>-
    private void OnExecute()
    {
        controller.OnRemoveItem();
    }

    /// <summary>
    /// 취소 버튼
    /// </summary>
    private void OnCancel()
    {
        controller.OnClose();
    }

    /// <summary>
    /// 수량 증가 버튼
    /// </summary>
    private void OnIncreaseButton()
    {
        controller.AdjustQuantity(+1);
        quantitySlider.value = controller.SettingQuantity; // 슬라이더 동기화
    }

    /// <summary>
    /// 수량 감소 버튼
    /// </summary>
    private void OnDecreaseButton()
    {
        controller.AdjustQuantity(-1);
        quantitySlider.value = controller.SettingQuantity; // 슬라이더 동기화
    }
    #endregion

    #region 슬라이더 이벤트
    /// <summary>
    /// 슬라이더 값 변경 시
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        controller.SetQuantity((int)value);
    }
    #endregion

    #region UI 업데이트
    /// <summary>
    /// 수량 텍스트 업데이트
    /// </summary>
    public void UpdateQuantityUI(int quantity)
    {
        if (quantityTxt != null)
        {
            quantityTxt.text = quantity.ToString();
        }
    }
    #endregion
}