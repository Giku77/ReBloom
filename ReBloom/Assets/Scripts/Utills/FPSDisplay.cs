using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fpsText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.2f; 

    private float timer;
    private int frameCount;

    private void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime; 

        if (timer >= updateInterval)
        {
            float fps = frameCount / timer;
            if (fpsText != null)
            {
                fpsText.text = $"{fps:0.} FPS";
            }

            // 초기화
            frameCount = 0;
            timer = 0f;
        }
    }
}
