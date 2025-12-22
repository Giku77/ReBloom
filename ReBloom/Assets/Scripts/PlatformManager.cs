using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    public static PlatformManager Instance { get; private set; }

    [Header("Editor Testing")]
    [SerializeField] private bool overrideInEditor = false;
    [SerializeField] private DeviceType editorDeviceType = DeviceType.Desktop;

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
#if UNITY_EDITOR
        if (overrideInEditor)
        {
            CurrentDevice = editorDeviceType;
            Debug.Log($"[PlatformManager] 에디터 오버라이드: {editorDeviceType}");
        }
        else
        {
            CurrentDevice = SystemInfo.deviceType;
        }
#else
        CurrentDevice = SystemInfo.deviceType;
#endif

        IsMobile = CurrentDevice == DeviceType.Handheld;
        IsPC = CurrentDevice == DeviceType.Desktop;

        Debug.Log($"[PlatformManager] 감지된 플랫폼: {CurrentDevice}");
        Debug.Log($"[PlatformManager] IsMobile: {IsMobile}, IsPC: {IsPC}");
    }

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
