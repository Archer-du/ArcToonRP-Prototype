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

        RendererListHandle baseList;
        RendererListHandle outlineList;

        private static ShaderTagId[] baseShaderTagIds =
        {
            new("ToonBase"),
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };
        private static ShaderTagId[] outlineShaderTagIds =
        {
            new("ToonOutline"),
        };

        void Render(RenderGraphContext context)
        {
            context.cmd.BeginSample("Toon Outline");
            context.cmd.DrawRendererList(outlineList);
            context.cmd.EndSample("Toon Outline");
            context.cmd.BeginSample("Toon Base");
            context.cmd.DrawRendererList(baseList);
            context.cmd.EndSample("Toon Base");
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults,
            in CameraAttachmentHandles handles, in LightingDataHandles lightingData)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out OpaquePass pass, sampler);

            pass.outlineList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(outlineShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                })
            );
            pass.baseList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(baseShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                })
            );
            builder.ReadWriteTexture(handles.colorAttachment);
            builder.ReadWriteTexture(handles.depthAttachment);
            builder.ReadTexture(handles.stencilMask);
            builder.ReadTexture(lightingData.shadowMapHandles.directionalAtlas);
            builder.ReadTexture(lightingData.shadowMapHandles.spotAtlas);
            builder.ReadTexture(lightingData.shadowMapHandles.pointAtlas);
            
            builder.ReadBuffer(lightingData.directionalLightDataHandle);
            builder.ReadBuffer(lightingData.spotLightDataHandle);
            builder.ReadBuffer(lightingData.pointLightDataHandle);
            builder.ReadBuffer(lightingData.forwardPlusTileBufferHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.cascadeShadowDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.directionalShadowMatricesHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.spotShadowDataHandle);
            builder.ReadBuffer(lightingData.shadowMapHandles.pointShadowDataHandle);

            builder.SetRenderFunc<OpaquePass>(static (pass, context) => pass.Render(context));
        }
    }
}