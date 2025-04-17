using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SetupPass
    {
        CameraRenderer renderer;

        void Render(RenderGraphContext context) => renderer.RenderSetup();
        
        public static void Record(
            RenderGraph renderGraph, CameraRenderer renderer)
        {
            using RenderGraphBuilder builder =
                renderGraph.AddRenderPass("Setup", out SetupPass pass);
            pass.renderer = renderer;
            builder.SetRenderFunc<SetupPass>((pass, context) => pass.Render(context));
        }
    }
}