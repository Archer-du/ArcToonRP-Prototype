using System.Diagnostics;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class UnsupportedPass
    {
#if UNITY_EDITOR
        CameraRenderer renderer;

        void Render(RenderGraphContext context) => renderer.DrawUnsupportedGeometry();
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
#if UNITY_EDITOR
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                "Unsupported Shaders", out UnsupportedPass pass);
            pass.renderer = renderer;
            builder.SetRenderFunc<UnsupportedPass>(
                (pass, context) => pass.Render(context));
#endif
        }
    }
}