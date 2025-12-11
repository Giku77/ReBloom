using LineworkLite.FreeOutline;
using UnityEngine;

[DisallowMultipleComponent]
public class OutlineToggle : MonoBehaviour
{
    [SerializeField] private Outline outline;
    [SerializeField] private bool outlined = false;

    [SerializeField] private uint outlineLayerMask = 1u << 8; // 8번 레이어로 설정

    private Renderer[] renderers;
    private uint originalMask;


    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
            originalMask = renderers[0].renderingLayerMask;

        Apply();
    }

// #if UNITY_EDITOR
//     private void OnValidate()
//     {
//         if (!Application.isPlaying)
//             renderers = GetComponentsInChildren<Renderer>();

//         Apply();
//     }
// #endif

    private void Apply()
    {
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            uint mask = outlined
                ? (originalMask | outlineLayerMask)   // 켜기
                : (originalMask & ~outlineLayerMask); // 끄기

            r.renderingLayerMask = mask;
        }
    }

    public void SetOutlined(bool value, bool throughWalls = false)
    {
        outlined = value;
        if (outline != null)
        {
            outline.occlusion =
                throughWalls
                ? LineworkLite.Common.Utils.Occlusion.Always       // 스캔 중
                : LineworkLite.Common.Utils.Occlusion.WhenNotOccluded; // 평소
        }
        Apply();
    }
}
