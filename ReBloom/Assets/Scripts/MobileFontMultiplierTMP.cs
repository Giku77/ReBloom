using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class MobileFontMultiplierTMP : MonoBehaviour
{
    [SerializeField] float mobileMultiplier = 1.5f;

    TMP_Text tmp;
    float baseSize;
    bool applied;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        baseSize = tmp.fontSize;   // PC 기준값 저장
        ApplyOnce();
    }

    void OnEnable() => ApplyOnce();

    void ApplyOnce()
    {
        if (applied) return;
        applied = true;

        if (Application.isMobilePlatform)
            tmp.fontSize = baseSize * mobileMultiplier;
        else
            tmp.fontSize = baseSize;
    }
}
