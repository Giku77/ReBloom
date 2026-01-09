using UnityEngine;
using TMPro;

public class NameTagUI : MonoBehaviour
{
    [SerializeField] private PlayerName playerName;
    [SerializeField] private TMP_Text nameText;

    private void Awake()
    {
        if (playerName == null) playerName = GetComponentInParent<PlayerName>();
    }

    private void OnEnable()
    {
        if (playerName != null)
            playerName.Name.OnValueChanged += OnNameChanged;
    }

    private void OnDisable()
    {
        if (playerName != null)
            playerName.Name.OnValueChanged -= OnNameChanged;
    }

    private void Start()
    {
        // 스폰 직후 초기 표시
        if (playerName != null)
            nameText.text = playerName.Name.Value.ToString();
    }

    private void OnNameChanged(Unity.Collections.FixedString32Bytes oldV, Unity.Collections.FixedString32Bytes newV)
    {
        nameText.text = newV.ToString();
    }
}
