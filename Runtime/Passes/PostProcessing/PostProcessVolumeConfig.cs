using System;

namespace ArcToon.Passes.PostProcessing
{
    /// <summary>
    /// Abstract base class for all post-processing effect volume configs.
    /// Each concrete subclass holds the parameters for a single effect.
    /// Serialized polymorphically via [SerializeReference] in PostProcessConfig.
    ///
    /// Each VolumeConfig owns a Processor instance (lazily created on first access).
    /// PostProcessPass borrows the Processor each frame — no per-frame allocation needed.
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

        /// <summary>
        /// The processor instance owned by this config, lazily created on first access.
        /// </summary>
        [NonSerialized] private PostProcessor processor;
        public PostProcessor Processor => processor ??= CreateProcessor();

        /// <summary>
        /// Factory method: create the corresponding PostProcessor for this config.
        /// Each VolumeConfig subclass knows which Processor it pairs with.
        /// </summary>
        protected abstract PostProcessor CreateProcessor();
    }
}
