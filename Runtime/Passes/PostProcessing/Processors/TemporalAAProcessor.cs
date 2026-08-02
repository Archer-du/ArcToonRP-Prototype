using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// TAA (Temporal Anti-Aliasing) post-processor.
    ///
    /// Each frame the camera projection is jittered (driven by <see cref="ArcToon.CameraRenderer"/>),
    /// and this resolve reprojects the previous resolved frame (the history buffer) into the current
    /// frame using depth-based camera motion, then blends the two:
    ///   1. Reconstruct camera/static motion from depth + previous/current view-projection matrices.
    ///   2. Reproject and sample the history buffer (bilinear).
    ///   3. Clamp history to the current 3x3 neighborhood color box (suppresses ghosting).
    ///   4. Exponentially blend: out = alpha * current + (1 - alpha) * clampedHistory.
    /// The blended result is copied back into the history buffer for the next frame.
    ///
    /// The history buffer is owned privately by this processor. Per-object motion vectors, YCoCg /
    /// variance clipping, Catmull-Rom resampling and anti-flicker are layered on in later steps.
    /// </summary>
    public class TemporalAAProcessor : VolumePostProcessor<TemporalAAVolumeConfig>
    {
        public TemporalAAProcessor(TemporalAAVolumeConfig config) : base(config) { }

        // ---- Local pass indices (must match TemporalAA.shader pass order) ----
        private enum Pass
        {
            Copy = 0,
            Resolve = 1,
        }

        // ---- Internal resources (self-owned) ----
        private RTHandle historyRT;
        private const string HistoryRTName = "_TAA_History";

        // ---- Cached state ----
        private Vector2Int cachedSize;
        private bool historyValid;

        private Matrix4x4 invViewProjCurrent;
        private Matrix4x4 viewProjPrevious;
        private float frameInfluence;
        private float varianceClampScale;

        protected override string ShaderPath => InternalShader.Path.PostProcessTemporalAA;

        public override bool IsActive(CameraRenderer renderer)
        {
            return volumeConfig.enabled;
        }

        public override void Setup(CameraRenderer renderer)
        {
            base.Setup(renderer);

            var size = renderer.AttachmentSize;
            if (size != cachedSize)
            {
                // History reprojected from a different resolution is meaningless; re-prime it.
                historyValid = false;
                cachedSize = size;
            }

            // History matches the working color format so accumulation stays in linear HDR.
            var colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            var desc = new RenderTextureDescriptor(size.x, size.y, colorFormat, 0)
            {
                msaaSamples = 1
            };
            RenderingUtils.ReAllocateIfNeeded(
                ref historyRT, desc,
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: HistoryRTName);

            var taa = renderer.TemporalAAData;
            invViewProjCurrent = taa.InverseViewProjectionCurrent;
            viewProjPrevious = taa.ViewProjectionPrevious;
            frameInfluence = volumeConfig.frameInfluence;
            varianceClampScale = volumeConfig.varianceClampScale;
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            if (!historyValid)
            {
                // No previous frame to blend against yet: pass through and prime the history buffer.
                BlitUtils.BlitTexture(cmd, source, destination, material, (int)Pass.Copy);
                BlitUtils.CopyTexture(cmd, destination, historyRT, BlitUtils.BlitMode.Color);
                historyValid = true;
                return;
            }

            cmd.SetGlobalMatrix(InternalShader.PropertyID.TAAInvViewProjCurr, invViewProjCurrent);
            cmd.SetGlobalMatrix(InternalShader.PropertyID.TAAViewProjPrev, viewProjPrevious);
            cmd.SetGlobalFloat(InternalShader.PropertyID.TAAFrameInfluence, frameInfluence);
            cmd.SetGlobalFloat(InternalShader.PropertyID.TAAVarianceClampScale, varianceClampScale);
            cmd.SetGlobalTexture(InternalShader.PropertyID.TAAHistoryTexture, historyRT);

            BlitUtils.BlitTexture(cmd, source, destination, material, (int)Pass.Resolve);

            // Persist this frame's resolved color as the history for the next frame.
            BlitUtils.CopyTexture(cmd, destination, historyRT, BlitUtils.BlitMode.Color);
        }

        public override void Dispose()
        {
            historyRT?.Release();
            historyRT = null;
            historyValid = false;
        }
    }
}
