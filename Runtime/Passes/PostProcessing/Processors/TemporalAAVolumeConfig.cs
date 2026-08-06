using System;
using UnityEngine;

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

        [Header("Accumulation")]
        [Tooltip("Weight of the current frame in the exponential blend (alpha). History weight is " +
                 "(1 - alpha). Lower values accumulate more history: smoother and steadier, but more " +
                 "ghosting. Reference default ~0.1 (history weight ~0.9).")]
        [Range(0.02f, 0.5f)]
        public float frameInfluence = 0.1f;

        [Header("History Rectification")]
        [Tooltip("Variance clipping tightness (gamma). The history is clipped to the neighborhood's " +
                 "mean +- gamma * stdDev. Larger keeps more history (steadier, more ghosting); " +
                 "smaller rejects more (crisper, more flicker). Reference ~1.0 (range 0.75~1.25).")]
        [Range(0.5f, 2.0f)]
        public float varianceClampScale = 1.0f;

        [Header("Anti-Flicker")]
        [Tooltip("How strongly history trust is cut where its luma disagrees with the current sample " +
                 "(Lottes luminance-diff feedback). Higher reduces flicker/fireflies on high-contrast " +
                 "edges but slightly weakens accumulation. 0 disables it (fixed feedback).")]
        [Range(0.0f, 1.0f)]
        public float flickerReduction = 0.25f;

        protected override PostProcessor CreateProcessor() => new TemporalAAProcessor(this);
    }
}

