using System;
using ArcToon.Settings;

namespace ArcToon.Data
{
    /// <summary>
    /// Composite container for all persistent render resources.
    /// Each functional domain is managed by a dedicated sub-class.
    /// Passes access resources through the appropriate sub-container
    /// (e.g. resources.Camera.colorAttachment, resources.Shadows.directionalShadowAtlas).
    /// </summary>
    public class RenderResources : IDisposable
    {
        public CameraAttachments Camera { get; } = new();
        public ShadowResources Shadows { get; } = new();
        public LightingResources Lighting { get; } = new();
        public PostFXResources PostFX { get; } = new();
        public TransparencyResources Transparency { get; } = new();

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

        public void AllocateTransparencyResources(int width, int height, bool useHDR)
        {
            Transparency.Allocate(width, height, useHDR);
        }

        public void AllocatePostFXResources(int width, int height, bool useHDR)
        {
            PostFX.Allocate(width, height, useHDR);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            Camera.Dispose();
            Shadows.Dispose();
            Lighting.Dispose();
            PostFX.Dispose();
            Transparency.Dispose();
        }
    }
}