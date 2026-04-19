using System;
using UnityEngine;
using UnityEngine.Rendering;
using ArcToon.Utils;

namespace ArcToon.Passes.PostProcessing
{
    /// <summary>
    /// Abstract base class for all post-processing effect processors.
    /// Each processor reads from source, writes to destination.
    /// Internal complexity (Bloom pyramid, LUT bake, etc.) is fully encapsulated.
    /// </summary>
    public abstract class PostProcessor : IDisposable
    {
        public abstract ProfilingSampler Sampler { get; }

        /// <summary>
        /// Dedicated shader path for this processor (e.g. Hidden/ArcToon/PostProcess/Bloom).
        /// The base class manages lazy material acquisition via this path.
        /// </summary>
        protected abstract string ShaderPath { get; }

        /// <summary>
        /// Cached material for this processor. Lazily initialized in Setup.
        /// Subclasses should use this instead of acquiring their own material.
        /// </summary>
        protected Material material;

        /// <summary>
        /// Check if this processor should run this frame.
        /// </summary>
        public abstract bool IsActive(CameraRenderer renderer);

        /// <summary>
        /// Per-frame setup: cache settings, allocate/resize internal RTs, etc.
        /// Called before Render only when IsActive returns true.
        /// Subclasses MUST call base.Setup to ensure material is initialized.
        /// </summary>
        public virtual void Setup(CameraRenderer renderer)
        {
            // Use explicit Unity null check — ??= won't catch destroyed-but-not-null objects.
            if (material == null)
                material = ShaderResourceManager.AcquireTransientMaterial(ShaderPath);
        }

        /// <summary>
        /// Core render: read from source, write to destination.
        /// This is the unified interface — no matter how complex the effect is internally,
        /// it always reads from source and writes to destination.
        /// </summary>
        public abstract void Render(CommandBuffer cmd, RTHandle source, RTHandle destination);

        public virtual void Dispose() { }
    }
}
