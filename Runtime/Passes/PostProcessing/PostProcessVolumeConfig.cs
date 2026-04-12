using System;

namespace ArcToon.Passes.PostProcessing
{
    /// <summary>
    /// Abstract base class for all post-processing effect volume configs.
    /// Each concrete subclass holds the parameters for a single effect.
    /// Serialized polymorphically via [SerializeReference] in PostProcessConfig.
    /// </summary>
    [Serializable]
    public abstract class PostProcessVolumeConfig
    {
        /// <summary>
        /// Execution order within the post-processing chain.
        /// Lower values execute first.
        /// </summary>
        public abstract int Order { get; }

        /// <summary>
        /// Whether this effect is enabled.
        /// </summary>
        public bool enabled = true;
    }
}
