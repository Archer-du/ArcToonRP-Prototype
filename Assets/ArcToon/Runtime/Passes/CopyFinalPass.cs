using ArcToon.Runtime.Overrides;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class CopyFinalPass
    {
        CameraRenderer renderer;

        CameraSettings.FinalBlendMode finalBlendMode;

        void Render(RenderGraphContext context)
        {
            renderer.CopyFinal(renderer.finalBufferId, finalBlendMode);
            // renderer.ExecuteBuffer();
        }

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer,
            CameraSettings.FinalBlendMode finalBlendMode)
        {
            using RenderGraphBuilder builder =
                renderGraph.AddRenderPass("Copy Final", out CopyFinalPass copyFinalPass);
            copyFinalPass.renderer = renderer;
            copyFinalPass.finalBlendMode = finalBlendMode;
            builder.SetRenderFunc<CopyFinalPass>((pass, context) => pass.Render(context));
        }
    }
}