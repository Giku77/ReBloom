using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    public static PlatformManager Instance { get; private set; }

    public bool IsMobile { get; private set; }
    public bool IsPC { get; private set; }
    public DeviceType CurrentDevice { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            DetectPlatform();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void DetectPlatform()
    {
        CurrentDevice = SystemInfo.deviceType;
        IsMobile = CurrentDevice == DeviceType.Handheld;
        IsPC = CurrentDevice == DeviceType.Desktop;

        Debug.Log($"[PlatformManager] 감지된 플랫폼: {CurrentDevice}");
        Debug.Log($"[PlatformManager] IsMobile: {IsMobile}, IsPC: {IsPC}");
    }

    // 사용 예시
    public void ApplyPlatformSettings()
    {
        if (IsMobile)
        {
            // 모바일 최적화 설정
        }
        else if (IsPC)
        {
            // PC 최적화 설정
        }
    }
}
