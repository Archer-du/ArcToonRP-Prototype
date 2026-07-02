using ArcToon.Data;
using ArcToon.Utils;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes
{
    /// <summary>
    /// Renders GeometryDebug-tagged geometry into the camera color attachment, replacing the
    /// normal geometry + post-process chain when a geometry debug mode is active.
    /// The selected mode is driven by the global _GeometryDebugMode uniform (see CameraDebugger).
    /// Only materials that provide a "GeometryDebug" pass are drawn; others are absent.
    /// </summary>
    public class GeometryDebugPass : RenderPassBase
    {
        public override string Name => "Geometry Debug";

        public GeometryDebugPass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        private RendererList debugList;

        public override void SetupRendererList(ScriptableRenderContext context)
        {
            debugList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.GeometryDebug, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.all,
            });
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetGlobalInteger(InternalShader.PropertyID.GeometryDebugMode, (int)DebuggerSingleton.GeometryDebugMode);

            commandBuffer.SetRenderTarget(
                resources.Camera.colorAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                resources.Camera.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            commandBuffer.DrawRendererList(debugList);

            // Post-processing is skipped in debug mode; hand the color attachment to the back buffer directly.
            resources.PostFX.postFXResult = resources.Camera.colorAttachment;
        }
    }
}
