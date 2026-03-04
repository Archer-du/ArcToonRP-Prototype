using ArcToon.Runtime.Utils;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public class GeometryOutlinePass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Geometry Outline");
        
        private RendererListHandle outlineList;
        
        public override bool AllowCulling() => true;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.BeginSample("Toon Outline");
            commandBuffer.DrawRendererList(outlineList);
            commandBuffer.EndSample("Toon Outline");
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            outlineList = renderGraph.CreateRendererList(new RendererListDesc(InternalShader.TagId.GeometryOutline, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = renderer.RenderPhase == RenderPhase.Transparent ? RenderQueueRange.transparent : RenderQueueRange.opaque,
            });
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.UseRendererList(outlineList);
            
            // general
            builder.ReadWriteTexture(resourceHandle.colorAttachment);
            builder.ReadWriteTexture(resourceHandle.depthAttachment);
            
            if (resourceHandle.colorCopy.IsValid())
            {
                builder.ReadTexture(resourceHandle.colorCopy);
            }
            if (resourceHandle.preDepthStencil.IsValid())
            {
                builder.ReadTexture(resourceHandle.preDepthStencil);
            }
            builder.ReadTexture(resourceHandle.stencilMask);
        }
    }
}