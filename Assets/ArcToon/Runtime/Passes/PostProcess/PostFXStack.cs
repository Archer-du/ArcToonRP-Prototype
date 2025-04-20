using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using static ArcToon.Runtime.Settings.PostFXSettings;

namespace ArcToon.Runtime.Passes.PostProcess
{
    public class PostFXStack
    {
        public PostFXSettings settings;
        public CameraRenderer.FXAARuntimeConfig fxaaConfig;

        public bool useHDR;

        public Vector2Int bufferSize;

        private int fxSourceId = Shader.PropertyToID("_PostFXSource");

        public enum Pass
        {
            BloomPrefilter,
            BloomPrefilterFireflies,
            BloomHorizontal,
            BloomVertical,
            BloomAdditive,
            BloomAdditiveFinal,
            BloomScatter,
            BloomScatterFinal,

            ColorGradingOnly,
            ColorGradingReinhard,
            ColorGradingNeutral,
            ColorGradingACES,
            ColorGradingApply,

            Copy,

            FXAA,
        }

        public void Setup(
            Vector2Int bufferSize,
            PostFXSettings settings,
            bool useHDR,
            CameraRenderer.FXAARuntimeConfig fxaaConfig)
        {
            this.settings = settings;
            this.useHDR = useHDR;
            this.fxaaConfig = fxaaConfig;
            this.bufferSize = bufferSize;
        }


        public void Draw(CommandBuffer commandBuffer, RenderTargetIdentifier srcRT, RenderTargetIdentifier dstRT, Pass pass)
        {
            commandBuffer.SetGlobalTexture(fxSourceId, srcRT);
            commandBuffer.SetRenderTarget(
                dstRT,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, settings.PostProcessStackMaterial, (int)pass,
                MeshTopology.Triangles, 3
            );
        }
    }
}