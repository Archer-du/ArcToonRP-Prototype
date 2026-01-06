using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class TransparentPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Transparent");
        
        private static ShaderTagId[] backFaceShaderTagIds =
        {
            new("ToonForwardTransparentBackFace"),
        };
        private static ShaderTagId[] frontFaceShaderTagIds =
        {
            new("ToonForwardTransparentFrontFace"),
            new("ToonForward"),
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };
        private static ShaderTagId[] outlineShaderTagIds =
        {
            new("GeometryOutline"),
        };

        RendererListHandle baseList;
        RendererListHandle frontFaceList;
        RendererListHandle backFaceList;
        RendererListHandle outlineList;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.BeginSample("Toon Base");
            commandBuffer.DrawRendererList(backFaceList);
            commandBuffer.DrawRendererList(frontFaceList);
            commandBuffer.EndSample("Toon Base");
            
            commandBuffer.BeginSample("Toon Outline");
            commandBuffer.DrawRendererList(outlineList);
            commandBuffer.EndSample("Toon Outline");
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            outlineList = renderGraph.CreateRendererList(new RendererListDesc(outlineShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
            });
            backFaceList = renderGraph.CreateRendererList(new RendererListDesc(backFaceShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
            frontFaceList = renderGraph.CreateRendererList(new RendererListDesc(frontFaceShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.UseRendererList(outlineList);
            builder.UseRendererList(backFaceList);
            builder.UseRendererList(frontFaceList);
            
            builder.ReadWriteTexture(resourceHandle.colorAttachment);
            builder.ReadWriteTexture(resourceHandle.depthAttachment);
            builder.ReadTexture(resourceHandle.stencilMask);
            if (resourceHandle.colorCopy.IsValid())
            {
                builder.ReadTexture(resourceHandle.colorCopy);
            }
            if (resourceHandle.depthCopy.IsValid())
            {
                builder.ReadTexture(resourceHandle.depthCopy);
            }
            
            builder.ReadTexture(resourceHandle.shadowMapHandle.directionalAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.spotAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.pointAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.perObjectAtlas);

            builder.ReadBuffer(resourceHandle.lightDataDirectional);
            builder.ReadBuffer(resourceHandle.lightDataSpot);
            builder.ReadBuffer(resourceHandle.lightDataPoint);
            builder.ReadBuffer(resourceHandle.perObjectShadowCasterData);
            builder.ReadBuffer(resourceHandle.forwardPlusTileBuffer);
            
            builder.ReadBuffer(resourceHandle.shadowMapHandle.cascadeShadowDataHandle);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.directionalShadowMatricesHandle);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.spotShadowDataHandle);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.pointShadowDataHandle);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.perObjectShadowDataHandle);
        }
    }
}