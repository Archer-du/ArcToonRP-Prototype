using ArcToon.Utils;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// TAA (Temporal Anti-Aliasing) post-processor.
    ///
    /// Step 1 scaffolding: this resolve is a straight pass-through copy of the jittered
    /// source. It exists so the effect can be enabled in the post-process chain and the
    /// camera-side sub-pixel jitter (driven by <see cref="ArcToon.CameraRenderer"/>) can be
    /// verified independently: with TAA enabled the raw image visibly jitters frame to
    /// frame; with it disabled the image is stable. History reprojection and accumulation
    /// are added in later steps.
    /// </summary>
    public class TemporalAAProcessor : VolumePostProcessor<TemporalAAVolumeConfig>
    {
        public TemporalAAProcessor(TemporalAAVolumeConfig config) : base(config) { }

        // ---- Local pass indices (must match TemporalAA.shader pass order) ----
        private const int CopyPass = 0;

        protected override string ShaderPath => InternalShader.Path.PostProcessTemporalAA;

        public override bool IsActive(CameraRenderer renderer)
        {
            return volumeConfig.enabled;
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            BlitUtils.BlitTexture(cmd, source, destination, material, CopyPass);
        }
    }
}
