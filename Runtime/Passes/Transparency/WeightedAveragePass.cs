using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes.Transparency
{
    public class WeightedAveragePass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Transparent Weighted Average");

        private TextureHandle backgroundColor;
        
        private TextureHandle accumulateRGBA;
        private TextureHandle revealage;
        
        private RendererListHandle geometryList;
        
        public override bool AllowCulling() => true;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetRenderTarget(
                new RenderTargetIdentifier[]{ accumulateRGBA, revealage, }, 
                resourceHandle.depthAttachment);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, 
                new[]{ Color.clear, Color.white, });
            commandBuffer.DrawRendererList(geometryList);
                
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateRGBA, accumulateRGBA);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateComplexity, revealage);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.BackGroundColor, backgroundColor);
            RenderTextureHelpers.CopyTexture(commandBuffer, resourceHandle.colorAttachment, backgroundColor, RenderTextureHelpers.BlitMode.Color);
                
            commandBuffer.SetRenderTarget(
                resourceHandle.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resourceHandle.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(false, true, Color.clear);
            // TODO: config
            commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 3);
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            geometryList = renderGraph.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardWeightedAverage, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
            accumulateRGBA = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
            {
                name = "Weighted Average Accumulate RGBA",
                colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            });
            revealage = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
            {
                name = "Weighted Average Revealage",
                format = GraphicsFormat.R16_UNorm,
            });
            backgroundColor = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
            {
                name = "Weighted Average Background Color",
                colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            });
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.UseRendererList(geometryList);
            builder.ReadWriteTexture(revealage);
            builder.ReadWriteTexture(accumulateRGBA);
            builder.ReadWriteTexture(backgroundColor);
            
            // general
            builder.ReadWriteTexture(resourceHandle.colorAttachment);
            builder.ReadWriteTexture(resourceHandle.depthAttachment);
            
            if (resourceHandle.preDepthStencil.IsValid())
            {
                builder.ReadTexture(resourceHandle.preDepthStencil);
            }
            builder.ReadTexture(resourceHandle.stencilMask);
            
            builder.ReadTexture(resourceHandle.shadowMapHandle.directionalAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.spotAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.pointAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.perObjectAtlas);

            builder.ReadBuffer(resourceHandle.lightDataDirectional);
            builder.ReadBuffer(resourceHandle.lightDataSpot);
            builder.ReadBuffer(resourceHandle.lightDataPoint);
            builder.ReadBuffer(resourceHandle.perObjectShadowCasterData);
            builder.ReadBuffer(resourceHandle.forwardPlusTileBuffer);
            
            builder.ReadBuffer(resourceHandle.shadowMapHandle.cascadeShadowData);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.directionalShadowMatrices);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.spotShadowData);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.pointShadowData);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.perObjectShadowData);
        }
    }
}