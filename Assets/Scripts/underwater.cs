// Underwater.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Underwater : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        public Color color = new Color(0.1f, 0.5f, 0.6f, 1f);
        public float distance = 10f;
        [Range(0, 1)] public float alpha = 0.3f;
        public float refraction = 0.1f;
        public Texture normalmap;
        public Vector4 UV = new Vector4(1, 1, 0.2f, 0.1f);
    }

    public Settings settings = new Settings();

    class UnderwaterPass : ScriptableRenderPass
    {
        private Settings settings;
        private RTHandle source;
        private RTHandle tempTexture;
        private string profilerTag;

        public UnderwaterPass(string tag, Settings settings)
        {
            this.settings = settings;
            profilerTag = tag;
        }

        public void Setup(RTHandle cameraTarget)
        {
            source = cameraTarget;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (source == null) return;
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(
                ref tempTexture,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_UnderwaterTemp"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.material == null || source == null) return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            settings.material.SetFloat("_dis", settings.distance);
            settings.material.SetFloat("_alpha", settings.alpha);
            settings.material.SetColor("_color", settings.color);
            settings.material.SetTexture("_NormalMap", settings.normalmap);
            settings.material.SetFloat("_refraction", settings.refraction);
            settings.material.SetVector("_normalUV", settings.UV);

            Blitter.BlitCameraTexture(cmd, source, tempTexture);
            Blitter.BlitCameraTexture(cmd, tempTexture, source, settings.material, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            if (tempTexture != null)
            {
                tempTexture.Release();
                tempTexture = null;
            }
        }
    }

    private UnderwaterPass pass;

    public override void Create()
    {
        pass = new UnderwaterPass("UnderwaterPass", settings);
        pass.renderPassEvent = settings.renderPassEvent;
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        pass.Setup(renderer.cameraColorTargetHandle);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }
}