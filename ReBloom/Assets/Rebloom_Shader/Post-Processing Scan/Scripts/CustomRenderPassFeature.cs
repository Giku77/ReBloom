using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CustomRenderPassSettings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public int materialPassIndex = 0;
    }

    [SerializeField]
    public CustomRenderPassSettings settings = new CustomRenderPassSettings();

    private CustomRenderPass pass;

    public override void Create()
    {
        if (settings.material == null)
            return;

        pass = new CustomRenderPass(
            settings.material,
            settings.renderPassEvent,
            settings.materialPassIndex
        );
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null || pass == null)
            return;

        // Game/Scene 뷰 카메라만 처리 (Preview 카메라 제외)
        if (renderingData.cameraData.cameraType == CameraType.Preview)
            return;

        pass.Setup(settings.material, settings.materialPassIndex);
        renderer.EnqueuePass(pass);
    }
}