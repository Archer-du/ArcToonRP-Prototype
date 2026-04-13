using System;
using ArcToon.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    /// <summary>
    /// Lightweight pass base for direct CommandBuffer execution mode.
    /// Lifecycle mirrors URP's ScriptableRenderPass event model:
    ///
    /// ┌─── Configuration Phase (all passes complete before any execution) ───┐
    /// │  ① Initialize(resources, renderer)  — inject dependencies, cache settings
    /// │  ② SetupResource(cmd)               — allocate/configure RTs, set global properties
    /// │  ③ SetupRendererList(context)        — create RendererLists
    /// └──────────────────────────────────────────────────────────────────────┘
    /// ┌─── Execution Phase (passes execute in order) ────────────────────────┐
    /// │  ④ Execute(cmd, context)             — core rendering logic
    /// └──────────────────────────────────────────────────────────────────────┘
    /// ┌─── Cleanup Phase (after all passes have executed) ───────────────────┐
    /// │  ⑤ CleanupResource(cmd)             — per-frame temp resource release, reset state
    /// └──────────────────────────────────────────────────────────────────────┘
    /// ┌─── Dispose Phase (pipeline destruction) ─────────────────────────────┐
    /// │  ⑥ Dispose()                         — release persistent resources
    /// └──────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public abstract class RenderPassBase : IDisposable
    {
        public abstract string Name { get; }

        protected RenderResources resources;

        protected CameraRenderer renderer;

        protected Camera Camera => renderer.RenderCamera;
        protected Vector2Int AttachmentSize => renderer.AttachmentSize;

        /// <summary>
        /// Initialize pass state from external dependencies.
        /// Called each frame before SetupResource.
        /// </summary>
        public virtual void Initialize(RenderResources resources, CameraRenderer renderer)
        {
            this.resources = resources;
            this.renderer = renderer;
        }

        /// <summary>
        /// Allocate or configure render targets and set global shader properties.
        /// Called after all passes have been initialized, before any Execute.
        /// Analogous to URP's OnCameraSetup.
        /// </summary>
        public virtual void SetupResource(CommandBuffer cmd) { }

        /// <summary>
        /// Create RendererLists and other per-frame resources.
        /// Called after SetupResource, before Execute.
        /// </summary>
        public virtual void SetupRendererList(ScriptableRenderContext context) { }

        /// <summary>
        /// Execute rendering commands.
        /// </summary>
        public abstract void Execute(CommandBuffer cmd, ScriptableRenderContext context);

        /// <summary>
        /// Per-frame cleanup: release temporary resources, clear global properties.
        /// Called after all passes have executed.
        /// Analogous to URP's OnCameraCleanup.
        /// </summary>
        public virtual void CleanupResource(CommandBuffer cmd) { }

        /// <summary>
        /// Release persistent resources (RTHandles, Materials, Buffers).
        /// Called when the pass or pipeline is destroyed.
        /// </summary>
        public virtual void Dispose() { }
    }
}
