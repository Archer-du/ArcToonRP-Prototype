using System;
using ArcToon.Passes;
using UnityEngine.Rendering;

namespace ArcToon.Data
{
    /// <summary>
    /// Shared post-processing resources.
    /// Currently holds the <see cref="postFXResult"/> pointer that links
    /// <see cref="ArcToon.Passes.PostProcessPass"/> and <see cref="BackBufferPass"/>.
    /// Intermediate RTHandles (bloom chain, color LUT, etc.) are now privately owned by
    /// each <see cref="ArcToon.Passes.PostProcessing.PostProcessor"/>.
    /// </summary>
    public class PostFXResources : IDisposable
    {
        /// <summary>
        /// Final post-processing output, points to the last active PostFX result.
        /// Used by CopyFinalPass.
        /// </summary>
        public RTHandle postFXResult;

        public void Dispose()
        {
            // postFXResult is an alias handle owned by either PostProcessPass (its temp buffers)
            // or CameraAttachments (colorAttachment passthrough when no processor is active);
            // nothing to release here.
        }
    }
}
