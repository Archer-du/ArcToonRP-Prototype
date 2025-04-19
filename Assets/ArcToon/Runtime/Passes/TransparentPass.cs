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

        RendererListHandle list;

        private static ShaderTagId[] shaderTagIds =
        {
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };

        void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults,
            in CameraAttachmentTextureData textureData, in LightDataHandles lightData)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out TransparentPass pass, sampler);

            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(shaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonTransparent,
                    renderQueueRange = RenderQueueRange.transparent,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                }));
            builder.ReadWriteTexture(textureData.colorAttachment);
            builder.ReadWriteTexture(textureData.depthAttachment);
            if (textureData.colorCopy.IsValid())
            {
                builder.ReadTexture(textureData.colorCopy);
            }
            if (textureData.depthCopy.IsValid())
            {
                builder.ReadTexture(textureData.depthCopy);
            }
            builder.ReadBuffer(lightData.directionalLightDataHandle);
            builder.ReadBuffer(lightData.spotLightDataHandle);
            builder.ReadBuffer(lightData.pointLightDataHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.cascadeShadowDataHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.directionalShadowMatricesHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.spotShadowDataHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.pointShadowDataHandle);

            builder.SetRenderFunc<TransparentPass>(static (pass, context) => pass.Render(context));
        }
    }
}