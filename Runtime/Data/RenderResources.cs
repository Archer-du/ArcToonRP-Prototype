using System;
using ArcToon.Settings;

namespace ArcToon.Data
{
    /// <summary>
    /// Composite container for shared render resources (cross-pass).
    /// Each functional domain is managed by a dedicated sub-class.
    /// Pass-exclusive resources are owned by the pass itself (e.g. TransparentPass).
    /// Passes access shared resources through the appropriate sub-container
    /// (e.g. resources.Camera.colorAttachment, resources.Shadows.directionalShadowAtlas).
    /// </summary>
    public class RenderResources : IDisposable
    {
        public CameraAttachments Camera { get; } = new();
        public ShadowResources Shadows { get; } = new();
        public LightingResources Lighting { get; } = new();
        public PostFXResources PostFX { get; } = new();

        private bool disposed;

        public void AllocateCameraResources(int width, int height, bool useHDR)
        {
            Camera.Allocate(width, height, useHDR);
        }

        public void AllocateShadowResources(ShadowSettings settings)
        {
            Shadows.Allocate(settings);
        }

        public void AllocateLightingResources()
        {
            Lighting.Allocate();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            Camera.Dispose();
            Shadows.Dispose();
            Lighting.Dispose();
            PostFX.Dispose();
        }
    }
}