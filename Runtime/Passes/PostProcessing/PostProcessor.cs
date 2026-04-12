using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing
{
    /// <summary>
    /// Abstract base class for all post-processing effect processors.
    /// Each processor reads from source, writes to destination.
    /// Internal complexity (Bloom pyramid, LUT bake, etc.) is fully encapsulated.
    /// </summary>
    public abstract class PostProcessor : IDisposable
    {
        /// <summary>
        /// Display name for profiling.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Execution order within the post-processing chain.
        /// Lower values execute first. Should match the corresponding VolumeConfig.Order.
        /// </summary>
        public abstract int Order { get; }

        /// <summary>
        /// Check if this processor should run this frame.
        /// </summary>
        public abstract bool IsActive(PostProcessConfig config, CameraRenderer renderer);

        /// <summary>
        /// Per-frame setup: cache settings, allocate/resize internal RTs, etc.
        /// Called before Render only when IsActive returns true.
        /// </summary>
        public virtual void Setup(PostProcessConfig config, CameraRenderer renderer) { }

        /// <summary>
        /// Core render: read from source, write to destination.
        /// This is the unified interface — no matter how complex the effect is internally,
        /// it always reads from source and writes to destination.
        /// </summary>
        public abstract void Render(CommandBuffer cmd, RTHandle source, RTHandle destination);

        public virtual void Dispose() { }
    }
}
