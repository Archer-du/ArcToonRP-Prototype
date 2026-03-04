using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class OpaquePass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Opaque");

        private static ShaderTagId[] baseShaderTagIds =
        {
            new("ToonForward"),
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };

        RendererListHandle baseList;

        public override bool AllowCulling() => true;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.BeginSample("Toon Base");
            commandBuffer.DrawRendererList(baseList);
            commandBuffer.EndSample("Toon Base");
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            baseList = renderGraph.CreateRendererList(new RendererListDesc(baseShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.UseRendererList(baseList);
            
            builder.ReadWriteTexture(resourceHandle.colorAttachment);
            builder.ReadWriteTexture(resourceHandle.depthAttachment);
            
            if (resourceHandle.colorCopy.IsValid())
            {
                builder.ReadTexture(resourceHandle.colorCopy);
            }
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