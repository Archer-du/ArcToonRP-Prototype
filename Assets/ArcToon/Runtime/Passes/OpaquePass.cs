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
            in CameraAttachmentTextureData textureData, in ShadowTextureData shadowData)
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
            builder.ReadTexture(shadowData.directionalAtlas);
            builder.ReadTexture(shadowData.spotAtlas);
            builder.ReadTexture(shadowData.pointAtlas);

            builder.SetRenderFunc<OpaquePass>(static (pass, context) => pass.Render(context));
        }
    }
}