using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes.Transparency
{
    public class WeightedAveragePass : RenderPassBase
    {
        public override string Name => "Transparent Weighted Average";

        private RendererList geometryList;

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            geometryList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardWeightedAverage, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetRenderTarget(
                new RenderTargetIdentifier[]{ resources.waAccumulateRGBA, resources.waRevealage, }, 
                resources.depthAttachment);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, 
                new[]{ Color.clear, Color.white, });
            commandBuffer.DrawRendererList(geometryList);
                
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateRGBA, resources.waAccumulateRGBA);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateComplexity, resources.waRevealage);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.BackGroundColor, resources.waBackgroundColor);
            RenderTextureHelpers.CopyTexture(commandBuffer, resources.colorAttachment, resources.waBackgroundColor, RenderTextureHelpers.BlitMode.Color);
                
            commandBuffer.SetRenderTarget(
                resources.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resources.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(false, true, Color.clear);
            // TODO: config
            commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 3);
        }
    }
}