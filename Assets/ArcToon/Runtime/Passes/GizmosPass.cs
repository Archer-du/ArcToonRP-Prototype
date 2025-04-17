using System.Diagnostics;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class GizmosPass
    {
#if UNITY_EDITOR
        CameraRenderer renderer;

        void Render(RenderGraphContext context)
        {
            if (renderer.useIntermediateBuffer)
            {
                renderer.CopyCameraTexture(CameraRenderer.depthAttachmentId, BuiltinRenderTextureType.CameraTarget,
                    CameraRenderer.CopyChannel.DepthAttachment);
                // renderer.ExecuteBuffer();
            }

            context.renderContext.DrawGizmos(renderer.camera, GizmoSubset.PreImageEffects);
            context.renderContext.DrawGizmos(renderer.camera, GizmoSubset.PostImageEffects);
        }
#endif
        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                using RenderGraphBuilder builder =
                    renderGraph.AddRenderPass("Gizmos", out GizmosPass gizmosPass);
                gizmosPass.renderer = renderer;
                builder.SetRenderFunc<GizmosPass>((pass, context) => pass.Render(context));
            }
#endif
        }
    }
}