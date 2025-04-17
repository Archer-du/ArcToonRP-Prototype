using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class VisibleGeometryPass
    {
        CameraRenderer renderer;

        bool useGPUInstancing;

        void Render(RenderGraphContext context) => renderer.DrawVisibleGeometry(useGPUInstancing);

        public static void Record(
            RenderGraph renderGraph, CameraRenderer renderer,
            bool useGPUInstancing)
        {
            using RenderGraphBuilder builder =
                renderGraph.AddRenderPass("Visible Geometry", out VisibleGeometryPass pass);
            pass.renderer = renderer;
            pass.useGPUInstancing = useGPUInstancing;
            builder.SetRenderFunc<VisibleGeometryPass>(
                (pass, context) => pass.Render(context));
        }
    }
}