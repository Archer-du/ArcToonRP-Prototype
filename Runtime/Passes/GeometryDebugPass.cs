using ArcToon.Data;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes
{
    /// <summary>
    /// Renders GeometryDebug-tagged geometry into the camera color attachment, replacing the
    /// normal geometry + post-process chain when a geometry debug mode is active.
    /// The selected mode is driven by the global _GeometryDebugMode uniform (see CameraDebugger).
    /// Only materials that provide a "GeometryDebug" pass are drawn; others are absent.
    /// In RegionID mode an on-screen legend (swatch + index per region) is drawn over the
    /// result, so each color maps to its index at a glance.
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

            if (DebuggerSingleton.GeometryDebugMode == DebuggerSingleton.GeometryDebug.RegionID)
            {
                DrawRegionLegend(commandBuffer);
            }

            // Post-processing is skipped in debug mode; hand the color attachment to the back buffer directly.
            resources.PostFX.postFXResult = resources.Camera.colorAttachment;
        }

        /// <summary>
        /// Draws the region palette legend (swatch + index per region) as a screen-space overlay.
        /// The legend shader renders both the semi-transparent backdrop strip and the swatches.
        /// </summary>
        private void DrawRegionLegend(CommandBuffer commandBuffer)
        {
            // Mirrors RegionIDLegendPass.hlsl layout: [margin][swatch][gap][digit][margin]
            // wide, margin + swatch per row, 8 rows (digit is 6px wide, 9px tall).
            float width = 12 + 14 + 6 + 6 + 12;
            float height = 12 + 14 * 8 + 12;

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.RegionLegendBackdrop, new Vector4(0, 0, width, height));

            commandBuffer.DrawScreenFilledTriangle(
                ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.RegionIDLegend), 0);
        }
    }
}
