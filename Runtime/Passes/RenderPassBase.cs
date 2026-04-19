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
    /// ┌─── Construction Phase (once, at pipeline creation) ──────────────────┐
    /// │  ctor(resources, renderer)          — inject dependencies (readonly)
    /// └──────────────────────────────────────────────────────────────────────┘
    /// ┌─── Configuration Phase (all passes complete before any execution) ───┐
    /// │  ① SetupFrameData(cmd)              — per-frame setup: cache camera /
    /// │                                        culling state, allocate NativeArrays,
    /// │                                        schedule jobs, configure render targets,
    /// │                                        set global shader properties, etc.
    /// │  ② SetupRendererList(context)       — create RendererLists
    /// └──────────────────────────────────────────────────────────────────────┘
    /// ┌─── Execution Phase (passes execute in order) ────────────────────────┐
    /// │  ③ Execute(cmd, context)            — core rendering logic
    /// └──────────────────────────────────────────────────────────────────────┘
    /// ┌─── Cleanup Phase (after all passes have executed) ───────────────────┐
    /// │  ④ CleanupFrameData(cmd)            — per-frame temp resource release, reset state
    /// └──────────────────────────────────────────────────────────────────────┘
    /// ┌─── Dispose Phase (pipeline destruction) ─────────────────────────────┐
    /// │  ⑤ Dispose()                        — release persistent resources
    /// └──────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public abstract class RenderPassBase : IDisposable
    {
        public abstract string Name { get; }

        protected readonly RenderResources resources;

        protected readonly CameraRenderer renderer;

        protected Camera Camera => renderer.RenderCamera;
        protected Vector2Int AttachmentSize => renderer.AttachmentSize;

        protected RenderPassBase(RenderResources resources, CameraRenderer renderer)
        {
            this.resources = resources;
            this.renderer = renderer;
        }

        /// <summary>
        /// Per-frame setup. Analogous to URP's OnCameraSetup.
        /// Use this hook for all per-frame preparation work: caching per-frame state
        /// derived from camera / culling results, allocating NativeArrays, scheduling
        /// jobs, allocating / configuring render targets, setting global shader
        /// properties, etc.
        /// Called before any Execute of this pass or later passes.
        /// </summary>
        public virtual void SetupFrameData(CommandBuffer cmd) { }

        /// <summary>
        /// Create RendererLists and other per-frame resources.
        /// Called after SetupFrameData, before Execute.
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
        public virtual void CleanupFrameData(CommandBuffer cmd) { }

        /// <summary>
        /// Release persistent resources (RTHandles, Materials, Buffers).
        /// Called when the pass or pipeline is destroyed.
        /// </summary>
        public virtual void Dispose() { }
    }
}
