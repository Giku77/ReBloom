using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class CustomRenderPass : ScriptableRenderPass
{
    private Material material;
    private int passIndex;

    public CustomRenderPass(Material mat, RenderPassEvent passEvent, int passIndex = 0)
    {
        material = mat;
        renderPassEvent = passEvent;
        this.passIndex = passIndex;
        requiresIntermediateTexture = true;

        // Depth 텍스처 필요하다고 명시
        ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
    }

    public void Setup(Material mat, int passIndex = 0)
    {
        material = mat;
        this.passIndex = passIndex;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (material == null)
            return;

        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        // 백버퍼 체크
        if (resourceData.isActiveTargetBackBuffer)
            return;

        // Scene/Game 뷰만 처리
        if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView)
            return;

        TextureHandle src = resourceData.activeColorTexture;
        if (!src.IsValid())
            return;

        // Descriptor 생성
        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;

        TextureHandle dst = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph,
            desc,
            "_ScanTempTexture",
            false
        );

        if (!dst.IsValid())
            return;

        // Blit
        var blitParams = new RenderGraphUtils.BlitMaterialParameters(src, dst, material, passIndex);
        renderGraph.AddBlitPass(blitParams, "Custom Scan Blit");

        resourceData.cameraColor = dst;
    }
}