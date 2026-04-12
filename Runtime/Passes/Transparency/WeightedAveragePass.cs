using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes.Transparency
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
                new RenderTargetIdentifier[]{ resources.Transparency.waAccumulateRGBA, resources.Transparency.waRevealage, }, 
                resources.Camera.depthAttachment);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, 
                new[]{ Color.clear, Color.white, });
            commandBuffer.DrawRendererList(geometryList);
                
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateRGBA, resources.Transparency.waAccumulateRGBA);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateComplexity, resources.Transparency.waRevealage);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.BackGroundColor, resources.Transparency.waBackgroundColor);
            RenderTextureHelpers.CopyTexture(commandBuffer, resources.Camera.colorAttachment, resources.Transparency.waBackgroundColor, RenderTextureHelpers.BlitMode.Color);
                
            commandBuffer.SetRenderTarget(
                resources.Camera.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resources.Camera.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(false, true, Color.clear);
            // TODO: config
            commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 3);
        }
    }
}