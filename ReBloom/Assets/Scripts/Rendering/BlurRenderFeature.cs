using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRenderFeature : ScriptableRendererFeature
{
    class BlurPass : ScriptableRenderPass
    {
        private readonly string profilerTag = "UI Background Blur";
        private Material blurMaterial;

        // RTHandle로 임시 텍스처를 관리
        private RTHandle tempRT;
        private static readonly int BlurTexID = Shader.PropertyToID("_BlurTex");

        public BlurPass(Material blurMaterial)
        {
            this.blurMaterial = blurMaterial;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        // 카메라마다 호출: 여기서 tempRT 사이즈를 카메라에 맞게 재할당
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(
                ref tempRT,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_BlurTempTex"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blurMaterial == null)
                return;

            var cmd = CommandBufferPool.Get(profilerTag);

            // 새 URP에서는 cameraColorTargetHandle 사용
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // 1) source -> tempRT 로 블러 적용
            // blurMaterial의 패스 인덱스(보통 0)
            Blitter.BlitCameraTexture(cmd, source, tempRT, blurMaterial, 0);

            // 2) 글로벌 텍스처로 등록 (ShaderGraph에서 _BlurTex 샘플)
            cmd.SetGlobalTexture(BlurTexID, tempRT);

            // 3) 필요하면 다시 tempRT -> source 로 되돌리기
            Blitter.BlitCameraTexture(cmd, tempRT, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // 여기서 tempRT Release 안 해도 됨
            // RTHandle은 ReAllocateIfNeeded가 알아서 처리
        }
   }

    [System.Serializable]
    public class BlurSettings
    {
        public Shader blurShader;
    }

    public BlurSettings settings = new BlurSettings();

    private BlurPass blurPass;
    private Material blurMaterial;

    public override void Create()
    {
        if (settings.blurShader != null)
        {
            blurMaterial = new Material(settings.blurShader);
            blurMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        blurPass = new BlurPass(blurMaterial);
    }

    // 카메라 렌더링 시 이 패스를 넣어주는 부분
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (blurMaterial == null)
            return;

        renderer.EnqueuePass(blurPass);
    }
}
