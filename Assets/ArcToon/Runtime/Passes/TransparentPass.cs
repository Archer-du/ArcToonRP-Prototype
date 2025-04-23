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
            new("ToonBase"),
        };

        void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults,
            in CameraAttachmentHandles handles, in LightingDataHandles lightingData)
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
            builder.ReadWriteTexture(handles.colorAttachment);
            builder.ReadWriteTexture(handles.depthAttachment);
            if (handles.colorCopy.IsValid())
            {
                builder.ReadTexture(handles.colorCopy);
            }
            if (handles.depthStencilCopy.IsValid())
            {
                builder.ReadTexture(handles.depthStencilCopy);
            }
            builder.ReadBuffer(lightingData.directionalLightDataHandle);
            builder.ReadBuffer(lightingData.spotLightDataHandle);
            builder.ReadBuffer(lightingData.pointLightDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.cascadeShadowDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.directionalShadowMatricesHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.spotShadowDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.pointShadowDataHandle);

            builder.SetRenderFunc<TransparentPass>(static (pass, context) => pass.Render(context));
        }
    }
}