using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SkyboxPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Skybox");

        RendererListHandle list;

        public override bool AllowCulling() => false;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.DrawRendererList(list);
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            list = renderGraph.CreateSkyboxRendererList(Camera);
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.UseRendererList(list);
            
            builder.ReadWriteTexture(resourceHandle.colorAttachment);
            builder.ReadTexture(resourceHandle.depthAttachment);
        }
    }
}