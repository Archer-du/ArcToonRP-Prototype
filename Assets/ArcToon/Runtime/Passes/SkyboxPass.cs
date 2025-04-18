using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SkyboxPass
    {
        static readonly ProfilingSampler sampler = new("Skybox");

        RendererListHandle list;

        void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults, 
            in CameraAttachmentTextureData textureData)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out SkyboxPass pass, sampler);

            pass.list = builder.UseRendererList(renderGraph.CreateSkyboxRendererList(camera));
            builder.ReadWriteTexture(textureData.colorAttachment);
            builder.ReadTexture(textureData.depthAttachment);

            builder.SetRenderFunc<SkyboxPass>(static (pass, context) => pass.Render(context));
        }
    }
}