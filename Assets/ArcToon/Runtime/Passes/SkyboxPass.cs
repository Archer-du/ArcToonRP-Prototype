using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SkyboxPass
    {
        static readonly ProfilingSampler sampler = new("Skybox");
        private RenderGraphResourceData resourceData;

        RendererListHandle list;

        void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(CameraRenderer renderer, RenderGraph renderGraph, Camera camera,
            RenderGraphResourceData resourceData,
            CullingResults cullingResults)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out SkyboxPass pass, sampler);

            pass.resourceData = resourceData;
            
            pass.list = builder.UseRendererList(renderGraph.CreateSkyboxRendererList(camera));
            
            builder.ReadWriteTexture(resourceData.colorAttachment);
            builder.ReadTexture(resourceData.depthAttachment);

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<SkyboxPass>(static (pass, context) => pass.Render(context));
        }
    }
}