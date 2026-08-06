using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Data
{
    /// <summary>
    /// Per-camera temporal state for TAA: the sub-pixel jitter and the view-projection matrices
    /// the resolve shader needs to reproject history.
    ///
    /// One instance lives on <see cref="ArcToon.CameraRenderer"/> and is advanced once per frame
    /// (only while TAA is active) via <see cref="Update"/>. A disabled effect leaves the
    /// projection untouched.
    /// </summary>
    public class TemporalAAData
    {
        // Number of jitter phases before the Halton sequence repeats. 8 matches UE4's default;
        // long enough to converge, short enough to avoid sample clustering during motion.
        private const int JitterPhaseCount = 8;

        private int frameIndex;

        /// <summary>Current sub-pixel jitter offset in NDC units ([-1,1] clip space), for this frame.</summary>
        public Vector2 JitterNDC { get; private set; }

        /// <summary>Jittered projection matrix (CPU convention) the geometry passes render with this frame.</summary>
        public Matrix4x4 JitteredProjectionMatrix { get; private set; }

        // GPU-convention view-projection matrices (reverse-Z / y-flip baked in) consumed by the
        // resolve shader to reconstruct camera motion:
        //   InverseViewProjectionCurrent - inverse of the *jittered* current VP. The depth buffer
        //     was rasterized with the jittered projection, so inverting it reconstructs the true
        //     world position from a depth sample.
        //   ViewProjectionPrevious - the *non-jittered* previous-frame VP. Reprojecting the world
        //     position through it yields where the surface sat in last frame's resolved history,
        //     which is jitter-free once accumulated.
        public Matrix4x4 InverseViewProjectionCurrent { get; private set; }
        public Matrix4x4 ViewProjectionPrevious { get; private set; }

        // Non-jittered current VP, retained to roll into ViewProjectionPrevious next frame.
        private Matrix4x4 viewProjectionCurrent;
        private bool hasPreviousFrame;

        /// <summary>
        /// Advance one jitter phase and rebuild the jitter + reprojection matrices for this camera.
        /// Called once per frame during camera setup, only when TAA is active.
        /// </summary>
        public void Update(Camera camera, Vector2Int attachmentSize)
        {
            frameIndex = (frameIndex + 1) % JitterPhaseCount;

            // Halton(2,3) remapped to [-0.5, 0.5] pixel offset. index+1 skips the degenerate
            // sample-0 at the origin.
            float haltonX = HaltonSequence.Get(frameIndex + 1, 2) - 0.5f;
            float haltonY = HaltonSequence.Get(frameIndex + 1, 3) - 0.5f;

            // A one-pixel NDC step spans 2/size (clip space is [-1,1] across the full extent),
            // so a +-0.5 pixel jitter maps to +-(1/size) in NDC.
            float offsetX = haltonX * (2.0f / attachmentSize.x);
            float offsetY = haltonY * (2.0f / attachmentSize.y);
            JitterNDC = new Vector2(offsetX, offsetY);

            // Left-multiply by a clip-space translation, matching the URP TAA jitter convention.
            JitteredProjectionMatrix = Matrix4x4.Translate(new Vector3(offsetX, offsetY, 0.0f)) * camera.projectionMatrix;

            // renderIntoTexture: true because every geometry pass renders into RTHandles, so these
            // GPU matrices match the projection Unity applied when writing the depth buffer we sample.
            Matrix4x4 view = camera.worldToCameraMatrix;
            Matrix4x4 gpuNonJitteredProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            Matrix4x4 gpuJitteredProjection = GL.GetGPUProjectionMatrix(JitteredProjectionMatrix, true);

            Matrix4x4 nonJitteredViewProjection = gpuNonJitteredProjection * view;

            // On the first frame there is no real previous frame: reuse the current VP so velocity
            // is zero and the resolve degenerates to the current sample.
            ViewProjectionPrevious = hasPreviousFrame ? viewProjectionCurrent : nonJitteredViewProjection;
            viewProjectionCurrent = nonJitteredViewProjection;
            hasPreviousFrame = true;

            InverseViewProjectionCurrent = (gpuJitteredProjection * view).inverse;
        }
    }
}
