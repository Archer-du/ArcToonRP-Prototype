using System;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// Volume config for the TAA (Temporal Anti-Aliasing) post-processing effect.
    ///
    /// TAA amortizes supersampling across frames: each frame the camera projection is
    /// jittered by a sub-pixel offset (Halton 2,3), and results are accumulated into a
    /// history buffer via reprojection. It resolves in linear HDR before tone mapping,
    /// so its Order is lower than Bloom (100) and ColorGrading (200).
    ///
    /// The camera-side jitter is driven by <see cref="ArcToon.CameraRenderer"/>, gated on
    /// an enabled instance of this config; the resolve itself lives in
    /// <see cref="TemporalAAProcessor"/>.
    /// </summary>
    [Serializable]
    public class TemporalAAVolumeConfig : PostProcessVolumeConfig
    {
        public override int Order => 50;

        protected override PostProcessor CreateProcessor() => new TemporalAAProcessor(this);
    }
}
