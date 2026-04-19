using System;
using UnityEngine;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// Volume config for the FXAA anti-aliasing post-processing effect.
    /// </summary>
    [Serializable]
    public class FXAAVolumeConfig : PostProcessVolumeConfig
    {
        public override int Order => 900;

        [Range(0.0312f, 0.0833f)]
        public float fixedThreshold = 0.0338f;

        [Range(0.063f, 0.333f)]
        public float relativeThreshold = 0.084f;

        [Range(0f, 1f)]
        public float subpixelBlending = 1f;

        public enum Quality { Low, Medium, High }

        public Quality quality = Quality.High;

        protected override PostProcessor CreateProcessor() => new FXAAProcessor(this);
    }
}
