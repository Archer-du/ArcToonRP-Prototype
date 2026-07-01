using System;
using UnityEngine;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// Volume config for the SMAA (Subpixel Morphological Anti-Aliasing) post-processing effect.
    ///
    /// Enhanced Subpixel Morphological Antialiasing by Jimenez et al. (2013).
    /// Pattern-based, post-process AA using three rendering passes:
    ///   1) Edge detection    (color or luma)
    ///   2) Blend weight calc (uses precomputed AreaTex + SearchTex)
    ///   3) Neighborhood blend
    ///
    /// Reference: http://www.iryoku.com/smaa/
    /// </summary>
    [Serializable]
    public class SMAAVolumeConfig : PostProcessVolumeConfig
    {
        public override int Order => 800;

        public enum Quality
        {
            Low,     // ~60% quality, fastest
            Medium,  // ~80% quality
            High,    // ~95% quality (default)
        }

        [Header("Quality")]
        public Quality quality = Quality.High;

        [Header("Precomputed Lookup Textures")]
        [Tooltip("SMAA precomputed area texture. Assign the AreaTex.tga shipped with this pipeline.")]
        public Texture2D areaTex;

        [Tooltip("SMAA precomputed search texture. Assign the SearchTex.tga shipped with this pipeline.")]
        public Texture2D searchTex;

        protected override PostProcessor CreateProcessor() => new SMAAProcessor(this);
    }
}
