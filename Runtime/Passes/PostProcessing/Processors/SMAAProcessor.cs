using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// SMAA (Enhanced Subpixel Morphological Antialiasing) post-processor.
    ///
    /// Three-pass pattern-based anti-aliasing, closely following Jimenez's reference
    /// and the URP port:
    ///   Pass 0: Edge Detection           (source   -> edgesTex)
    ///   Pass 1: Blending Weight Calc     (edgesTex -> blendTex, reads AreaTex + SearchTex)
    ///   Pass 2: Neighborhood Blending    (source + blendTex -> destination)
    ///
    /// Intermediate RTs (edgesTex/blendTex) are privately owned by this processor.
    /// Uses dedicated shader: Hidden/ArcToon/PostProcess/SMAA.
    ///
    /// Notes vs URP reference:
    /// - We omit the stencil optimization (URP uses a stencil buffer to cull non-edge
    ///   pixels on pass 2). The algorithm itself is still correct because
    ///   SMAAColorEdgeDetectionPS clips non-edge pixels. This is purely a perf tradeoff
    ///   we can revisit later.
    /// - Edge detection uses color (not luma/depth). Color catches chroma-only edges at
    ///   a small perf cost, matching URP's default.
    /// </summary>
    public class SMAAProcessor : VolumePostProcessor<SMAAVolumeConfig>
    {
        public SMAAProcessor(SMAAVolumeConfig config) : base(config) { }

        // ---- Local pass indices (must match SMAA.shader pass order) ----
        private enum Pass
        {
            EdgeDetection = 0,
            BlendWeights = 1,
            NeighborhoodBlending = 2,
        }

        // ---- Internal resources (self-owned) ----
        private RTHandle edgesRT;
        private RTHandle blendRT;

        // ---- Cached state ----
        private Vector2Int cachedSize;

        protected override string ShaderPath => InternalShader.Path.PostProcessSMAA;

        public override bool IsActive(CameraRenderer renderer)
        {
            if (!volumeConfig.enabled) return false;
            // Both lookup textures are mandatory; without them the blending weights pass is undefined.
            if (volumeConfig.areaTex == null || volumeConfig.searchTex == null) return false;
            return true;
        }

        public override void Setup(CameraRenderer renderer)
        {
            base.Setup(renderer);

            cachedSize = renderer.AttachmentSize;

            // Edge texture: 2 channels are enough (rg = horizontal/vertical edge flags).
            var edgesDesc = new RenderTextureDescriptor(
                cachedSize.x, cachedSize.y, GraphicsFormat.R8G8_UNorm, 0)
            {
                msaaSamples = 1
            };
            RenderingUtils.ReAllocateIfNeeded(
                ref edgesRT, edgesDesc,
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SMAA_EdgesTex");

            // Blend weights texture: RGBA8, needs point filtering in pass 2.
            var blendDesc = new RenderTextureDescriptor(
                cachedSize.x, cachedSize.y, GraphicsFormat.R8G8B8A8_UNorm, 0)
            {
                msaaSamples = 1
            };
            RenderingUtils.ReAllocateIfNeeded(
                ref blendRT, blendDesc,
                FilterMode.Point, TextureWrapMode.Clamp, name: "_SMAA_BlendTex");
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            ConfigureMaterial(cmd);

            // -------- Pass 0: Edge Detection (source -> edges) --------
            // Edges texture must be cleared to zero because the edge detection shader only writes
            // to pixels that are actually detected as edges (the rest will be discarded via clip()).
            cmd.SetRenderTarget(edgesRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1.0f, 0);
            BlitUtils.BlitTexture(cmd, source, edgesRT, material, (int)Pass.EdgeDetection);

            // -------- Pass 1: Blending Weights Calculation (edges -> blend) --------
            // Blend texture must be cleared too (pass 1 only writes detected-pattern pixels).
            cmd.SetRenderTarget(blendRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1.0f, 0);
            BlitUtils.BlitTexture(cmd, edgesRT, blendRT, material, (int)Pass.BlendWeights);

            // -------- Pass 2: Neighborhood Blending (source + blend -> destination) --------
            // Bind blend texture globally; BlitUtils binds source as _SourceTexture.
            cmd.SetGlobalTexture(InternalShader.PropertyID.SMAABlendTexture, blendRT);
            BlitUtils.BlitTexture(cmd, source, destination, material, (int)Pass.NeighborhoodBlending);
        }

        private void ConfigureMaterial(CommandBuffer cmd)
        {
            // RT metrics: SMAA uses (1/width, 1/height, width, height).
            cmd.SetGlobalVector(InternalShader.PropertyID.SMAAMetrics, new Vector4(
                1f / cachedSize.x, 1f / cachedSize.y, cachedSize.x, cachedSize.y));

            // Precomputed lookup tables shared by blending-weights pass.
            cmd.SetGlobalTexture(InternalShader.PropertyID.SMAAAreaTexture, volumeConfig.areaTex);
            cmd.SetGlobalTexture(InternalShader.PropertyID.SMAASearchTexture, volumeConfig.searchTex);

            // Quality preset keywords (mutually exclusive).
            bool low = volumeConfig.quality == SMAAVolumeConfig.Quality.Low;
            bool medium = volumeConfig.quality == SMAAVolumeConfig.Quality.Medium;
            bool high = volumeConfig.quality == SMAAVolumeConfig.Quality.High;
            cmd.SetKeyword(InternalShader.GlobalKeyword.SMAA_PRESET_LOW, low);
            cmd.SetKeyword(InternalShader.GlobalKeyword.SMAA_PRESET_MEDIUM, medium);
            cmd.SetKeyword(InternalShader.GlobalKeyword.SMAA_PRESET_HIGH, high);
        }

        public override void Dispose()
        {
            edgesRT?.Release();
            blendRT?.Release();
            edgesRT = null;
            blendRT = null;
        }
    }
}
