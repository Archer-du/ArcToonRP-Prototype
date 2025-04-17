using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
namespace ArcToon.Runtime.Passes
{
    public class PostFXPass
    {
        PostFXStack postFXStack;

        int Render(RenderGraphContext context) =>
            postFXStack.Render(CameraRenderer.colorAttachmentId);

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer, PostFXStack postFXStack)
        {
            using RenderGraphBuilder builder =
                renderGraph.AddRenderPass("Post FX", out PostFXPass postFXPass);
            postFXPass.postFXStack = postFXStack;
            builder.SetRenderFunc<PostFXPass>((pass, context) => renderer.finalBufferId = pass.Render(context));
        }
    }
}