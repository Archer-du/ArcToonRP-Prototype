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
        private static int sourceTextureID = Shader.PropertyToID("_PostFXSource");

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

        private Material postFXMaterial;
        public PostFXStack(Material postFXMaterial)
        {
            this.postFXMaterial = postFXMaterial;
        }
        public void Draw(CommandBuffer commandBuffer, RenderTargetIdentifier srcHandle, RenderTargetIdentifier dstHandle, Pass pass)
        {
            commandBuffer.SetGlobalTexture(sourceTextureID, srcHandle);
            commandBuffer.SetRenderTarget(dstHandle,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, postFXMaterial, (int)pass,
                MeshTopology.Triangles, 3
            );
        }
    }
}