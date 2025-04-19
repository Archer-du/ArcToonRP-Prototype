using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class OpaquePass
    {
        static readonly ProfilingSampler sampler = new("Opaque");

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
                sampler.name, out OpaquePass pass, sampler);

            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(shaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                }));
            builder.ReadWriteTexture(textureData.colorAttachment);
            builder.ReadWriteTexture(textureData.depthAttachment);
            builder.ReadTexture(lightData.shadowMapHandles.directionalAtlas);
            builder.ReadTexture(lightData.shadowMapHandles.spotAtlas);
            builder.ReadTexture(lightData.shadowMapHandles.pointAtlas);
            
            builder.ReadBuffer(lightData.directionalLightDataHandle);
            builder.ReadBuffer(lightData.spotLightDataHandle);
            builder.ReadBuffer(lightData.pointLightDataHandle);
            builder.ReadBuffer(lightData.tileBufferHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.cascadeShadowDataHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.directionalShadowMatricesHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.spotShadowDataHandle);
            builder.ReadBuffer(lightData.shadowMapHandles.pointShadowDataHandle);

            builder.SetRenderFunc<OpaquePass>(static (pass, context) => pass.Render(context));
        }
    }
}