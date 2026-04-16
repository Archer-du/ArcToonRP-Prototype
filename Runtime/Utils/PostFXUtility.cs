using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Utils
{
    /// <summary>
    /// Static utility for full-screen post-processing draw calls.
    /// Replaces the old instance-based PostFXStack class.
    /// </summary>
    public static class PostFXUtility
    {
        private static readonly int sourceTextureID = Shader.PropertyToID("_PostFXSource");

        /// <summary>
        /// Full-screen draw with explicit material and pass index.
        /// Sets the source texture as a global shader property, then draws a full-screen triangle.
        /// </summary>
        public static void Draw(CommandBuffer cmd,
            RenderTargetIdentifier src, RenderTargetIdentifier dst,
            Material material, int pass)
        {
            cmd.SetGlobalTexture(sourceTextureID, src);
            cmd.SetRenderTarget(dst,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawScreenFilledTriangle(material, pass);
        }
    }
}
