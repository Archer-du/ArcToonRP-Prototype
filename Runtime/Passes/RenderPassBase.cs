using ArcToon.Data;
using ArcToon.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    /// <summary>
    /// Lightweight pass base for direct CommandBuffer execution mode.
    /// Replaces the old RenderGraphPassBase — no RenderGraph dependency.
    /// </summary>
    public abstract class RenderPassBase
    {
        public abstract string Name { get; }

        protected RenderResources resources;

        protected CameraRenderer renderer;

        protected Camera Camera => renderer.RenderCamera;
        protected Vector2Int AttachmentSize => renderer.AttachmentSize;

        /// <summary>
        /// Setup pass state from external dependencies.
        /// Called each frame before Execute.
        /// </summary>
        public virtual void Setup(RenderResources resources, CameraRenderer renderer)
        {
            this.resources = resources;
            this.renderer = renderer;
        }

        /// <summary>
        /// Create RendererLists and other per-frame resources.
        /// Called after Setup, before Execute.
        /// </summary>
        public virtual void PrepareRendererLists(ScriptableRenderContext context) { }

        /// <summary>
        /// Execute rendering commands.
        /// </summary>
        public abstract void Execute(CommandBuffer cmd, ScriptableRenderContext context);
    }
}
