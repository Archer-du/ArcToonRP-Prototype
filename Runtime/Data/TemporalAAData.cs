using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Data
{
    /// <summary>
    /// Per-camera temporal state for TAA.
    ///
    /// Step 1 responsibility: drive the sub-pixel camera jitter. Each rendered frame advances
    /// a phase index through a Halton(2,3) low-discrepancy sequence, maps the sample into a
    /// sub-pixel NDC translation, and builds the jittered projection matrix the geometry passes
    /// render with. History-buffer reprojection state (previous/current non-jittered
    /// view-projection matrices) is added in later steps.
    ///
    /// One instance lives on <see cref="ArcToon.CameraRenderer"/>; jitter is only advanced on
    /// frames where TAA is active, so a disabled effect leaves the projection untouched.
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

        /// <summary>
        /// Advance one jitter phase and rebuild the jittered projection for the given camera.
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
        }
    }
}
