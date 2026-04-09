using ArcToon.Runtime.Utils;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class GeometryOutlinePass : RenderPassBase
    {
        public override string Name => "Geometry Outline";

        private RendererList outlineList;

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            outlineList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.GeometryOutline, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = renderer.RenderPhase == RenderPhase.Transparent ? RenderQueueRange.transparent : RenderQueueRange.opaque,
            });
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.BeginSample("Toon Outline");
            commandBuffer.DrawRendererList(outlineList);
            commandBuffer.EndSample("Toon Outline");
        }
    }
}