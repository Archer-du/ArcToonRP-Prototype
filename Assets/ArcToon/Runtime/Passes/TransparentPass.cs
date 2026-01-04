using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class TransparentPass
    {
        static readonly ProfilingSampler sampler = new("Transparent");
        private RenderGraphResourceData resourceData;

        RendererListHandle baseList;
        RendererListHandle frontFaceList;
        RendererListHandle backFaceList;
        RendererListHandle outlineList;
        
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

        void Render(RenderGraphContext context)
        {
            context.cmd.BeginSample("Toon Base");
            context.cmd.DrawRendererList(backFaceList);
            context.cmd.DrawRendererList(frontFaceList);
            context.cmd.EndSample("Toon Base");
            context.cmd.BeginSample("Toon Outline");
            context.cmd.DrawRendererList(outlineList);
            context.cmd.EndSample("Toon Outline");
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(CameraRenderer renderer, RenderGraph renderGraph, Camera camera,
            RenderGraphResourceData resourceData,
            CullingResults cullingResults,
            in LightingDataHandles lightingData)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out TransparentPass pass, sampler);
            
            pass.resourceData = resourceData;

            pass.outlineList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(outlineShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonTransparent,
                    renderQueueRange = RenderQueueRange.transparent,
                })
            );
            pass.backFaceList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(backFaceShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonTransparent,
                    renderQueueRange = RenderQueueRange.transparent,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                })
            );
            pass.frontFaceList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(frontFaceShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonTransparent,
                    renderQueueRange = RenderQueueRange.transparent,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                })
            );
            
            builder.ReadWriteTexture(resourceData.colorAttachment);
            builder.ReadWriteTexture(resourceData.depthAttachment);
            builder.ReadTexture(resourceData.stencilMask);
            if (resourceData.colorCopy.IsValid())
            {
                builder.ReadTexture(resourceData.colorCopy);
            }
            if (resourceData.depthCopy.IsValid())
            {
                builder.ReadTexture(resourceData.depthCopy);
            }
            
            builder.ReadTexture(lightingData.shadowMapHandles.directionalAtlas);
            builder.ReadTexture(lightingData.shadowMapHandles.spotAtlas);
            builder.ReadTexture(lightingData.shadowMapHandles.pointAtlas);
            builder.ReadTexture(lightingData.shadowMapHandles.perObjectAtlas);

            builder.ReadBuffer(lightingData.directionalLightDataHandle);
            builder.ReadBuffer(lightingData.spotLightDataHandle);
            builder.ReadBuffer(lightingData.pointLightDataHandle);
            builder.ReadBuffer(lightingData.perObjectShadowCasterDataHandle);
            builder.ReadBuffer(lightingData.forwardPlusTileBufferHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.cascadeShadowDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.directionalShadowMatricesHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.spotShadowDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.pointShadowDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.perObjectShadowDataHandle);

            builder.SetRenderFunc<TransparentPass>(static (pass, context) => pass.Render(context));
        }
    }
}