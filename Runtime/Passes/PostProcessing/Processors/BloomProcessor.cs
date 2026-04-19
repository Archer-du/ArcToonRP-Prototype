using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// Bloom post-processor.
    /// Ported from the old BloomPass — rendering logic is identical.
    /// All intermediate RTs (prefilter, pyramid) are privately owned.
    /// Uses its own dedicated shader: Hidden/ArcToon/PostProcess/Bloom.
    /// </summary>
    public class BloomProcessor : VolumePostProcessor<BloomVolumeConfig>
    {
        public BloomProcessor(BloomVolumeConfig config) : base(config) { }

        // ---- Local pass indices (must match Bloom.shader pass order) ----
        private enum Pass
        {
            Prefilter,
            PrefilterFireflies,
            Horizontal,
            Vertical,
            AdditiveCombine,
            AdditiveCombineFinal,
            ScatterCombine,
            ScatterCombineFinal,
        }

        // ---- Internal resources (self-owned) ----
        private const int MaxPyramidLevels = 16;
        private RTHandle prefilter;
        private RTHandle[] pyramid = new RTHandle[2 * MaxPyramidLevels];

        // ---- Cached state per frame ----
        private int stepCount;
        private bool useHDR;

        // ---- Shader property IDs ----
        private static readonly int bloomHighResTextureID = Shader.PropertyToID("_BloomHighResTexture");
        private static readonly int bloomThresholdID = Shader.PropertyToID("_BloomThreshold");
        private static readonly int bloomBicubicUpsamplingID = Shader.PropertyToID("_BloomBicubicUpsampling");
        private static readonly int bloomScaleID = Shader.PropertyToID("_BloomScale");
        private static readonly int bloomScatterID = Shader.PropertyToID("_BloomScatter");

        public override bool IsActive(CameraRenderer renderer)
        {
            if (!volumeConfig.enabled) return false;

            Vector2Int bufferSize = volumeConfig.ignoreRenderScale
                ? new Vector2Int(renderer.RenderCamera.pixelWidth, renderer.RenderCamera.pixelHeight)
                : renderer.AttachmentSize;

            return volumeConfig.maxIterations > 0
                && bufferSize.y >= volumeConfig.downscaleLimit * 4
                && bufferSize.x >= volumeConfig.downscaleLimit * 4;
        }

        protected override string ShaderPath => InternalShader.Path.PostProcessBloom;

        public override void Setup(CameraRenderer renderer)
        {
            base.Setup(renderer);
            useHDR = renderer.useHDR;

            // Compute buffer size (may differ from attachmentSize if ignoreRenderScale)
            Vector2Int bufferSize = volumeConfig.ignoreRenderScale
                ? new Vector2Int(renderer.RenderCamera.pixelWidth, renderer.RenderCamera.pixelHeight)
                : renderer.AttachmentSize;

            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

            // Allocate prefilter at half resolution
            bufferSize /= 2;
            {
                var prefilterDesc = new RenderTextureDescriptor(bufferSize.x, bufferSize.y, colorFormat, 0);
                RenderingUtils.ReAllocateIfNeeded(ref prefilter, prefilterDesc, name: "Bloom Prefilter");
            }

            // Allocate pyramid levels
            bufferSize /= 2;
            int pyramidIndex = 0;
            int i;
            for (i = 0; i < volumeConfig.maxIterations; i++, pyramidIndex += 2)
            {
                if (bufferSize.y < volumeConfig.downscaleLimit || bufferSize.x < volumeConfig.downscaleLimit)
                {
                    break;
                }

                var desc = new RenderTextureDescriptor(bufferSize.x, bufferSize.y, colorFormat, 0);
                RenderingUtils.ReAllocateIfNeeded(ref pyramid[pyramidIndex], desc, name: "Bloom Pyramid H");
                RenderingUtils.ReAllocateIfNeeded(ref pyramid[pyramidIndex + 1], desc, name: "Bloom Pyramid V");
                bufferSize /= 2;
            }

            stepCount = i;
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            // ---- Prefilter ----
            cmd.SetGlobalVector(bloomThresholdID, GetKneeCurveData());

            BlitUtils.BlitTexture(cmd, source, prefilter, material,
                volumeConfig.fadeFireflies
                    ? (int)Pass.PrefilterFireflies
                    : (int)Pass.Prefilter);

            // ---- Downsample ----
            int dstPyramidIndex = 1;
            int srcPyramidIndex = 1;
            RTHandle srcHandle = prefilter;
            int i;
            for (i = 0; i < stepCount; i++)
            {
                BlitUtils.BlitTexture(cmd, srcHandle, pyramid[dstPyramidIndex - 1], material,
                    (int)Pass.Horizontal);
                BlitUtils.BlitTexture(cmd, pyramid[dstPyramidIndex - 1], pyramid[dstPyramidIndex], material,
                    (int)Pass.Vertical);
                srcPyramidIndex = dstPyramidIndex;
                srcHandle = pyramid[srcPyramidIndex];
                dstPyramidIndex += 2;
            }

            // ---- Upsample ----
            cmd.SetGlobalFloat(bloomBicubicUpsamplingID, volumeConfig.bicubicUpsampling ? 1f : 0f);
            int combinePass, finalPass;
            if (volumeConfig.mode == BloomVolumeConfig.Mode.Additive)
            {
                combinePass = (int)Pass.AdditiveCombine;
                finalPass = (int)Pass.AdditiveCombineFinal;
                cmd.SetGlobalFloat(bloomScaleID, volumeConfig.intensity);
            }
            else
            {
                combinePass = (int)Pass.ScatterCombine;
                finalPass = (int)Pass.ScatterCombineFinal;
                cmd.SetGlobalFloat(bloomScatterID, volumeConfig.scatter);
            }

            dstPyramidIndex -= 5;
            for (i -= 1; i > 0; i--)
            {
                cmd.SetGlobalTexture(bloomHighResTextureID, pyramid[dstPyramidIndex + 1]);
                BlitUtils.BlitTexture(cmd, pyramid[srcPyramidIndex], pyramid[dstPyramidIndex], material, combinePass);
                srcPyramidIndex = dstPyramidIndex;
                dstPyramidIndex -= 2;
            }

            // Final combine: blend bloom result with original source → destination
            cmd.SetGlobalTexture(bloomHighResTextureID, source);
            BlitUtils.BlitTexture(cmd, pyramid[srcPyramidIndex], destination, material, finalPass);
        }

        private Vector4 GetKneeCurveData()
        {
            Vector4 thresholdData;
            thresholdData.x = Mathf.GammaToLinearSpace(volumeConfig.threshold);
            thresholdData.y = thresholdData.x * volumeConfig.thresholdKnee;
            thresholdData.z = 2f * thresholdData.y;
            thresholdData.w = 1f / (4 * thresholdData.y + 0.00001f);
            thresholdData.y -= thresholdData.x;
            return thresholdData;
        }

        public override void Dispose()
        {
            prefilter?.Release();
            for (int i = 0; i < pyramid.Length; i++)
            {
                pyramid[i]?.Release();
            }
        }
    }
}
