using System;
using UnityEngine;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// Volume config for the Bloom post-processing effect.
    /// Mirrors the parameters from the old PostFXConfig.BloomSettings.
    /// </summary>
    [Serializable]
    public class BloomVolumeConfig : PostProcessVolumeConfig
    {
        public override int Order => 100;

        public enum Mode
        {
            Additive,
            Scattering
        }

        public Mode mode = Mode.Scattering;

        public bool ignoreRenderScale = true;

        [Range(0.05f, 0.95f)] public float scatter = 0.5f;

        [Range(0f, 16f)] public int maxIterations = 2;

        [Min(1f)] public int downscaleLimit = 10;

        [Min(0f)] public float threshold = 1f;

        [Range(0f, 1f)] public float thresholdKnee = 1f;

        [Min(0f)] public float intensity = 1f;

        public bool fadeFireflies = true;

        public bool bicubicUpsampling = true;

        protected override PostProcessor CreateProcessor() => new BloomProcessor(this);
    }
}
