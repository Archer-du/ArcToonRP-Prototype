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
    /// </summary>
    public class BloomProcessor : VolumePostProcessor<BloomVolumeConfig>
    {
        public BloomProcessor(BloomVolumeConfig config) : base(config) { }

        // ---- Internal resources (self-owned) ----
        private const int MaxPyramidLevels = 16;
        private RTHandle prefilter;
        private RTHandle[] pyramid = new RTHandle[2 * MaxPyramidLevels];

        // ---- Cached state per frame ----
        private Material material;
        private int stepCount;
        private bool useHDR;
        private Vector2Int attachmentSize;

        // ---- Shader property IDs ----
        private static readonly int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
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
                && volumeConfig.intensity > 0f
                && bufferSize.y >= volumeConfig.downscaleLimit * 4
                && bufferSize.x >= volumeConfig.downscaleLimit * 4;
        }

        public override void Setup(CameraRenderer renderer)
        {
            // Use explicit Unity null check — ??= won't catch destroyed-but-not-null objects
            if (material == null)
                material = ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.PostFXStack);
            useHDR = renderer.useHDR;
            attachmentSize = renderer.AttachmentSize;

            // Compute buffer size (may differ from attachmentSize if ignoreRenderScale)
            Vector2Int bufferSize = volumeConfig.ignoreRenderScale
                ? new Vector2Int(renderer.RenderCamera.pixelWidth, renderer.RenderCamera.pixelHeight)
                : attachmentSize;

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
            cmd.SetGlobalVector(bloomThresholdID, GetKneeCurveData(volumeConfig));

            PostFXUtility.Draw(cmd, source, prefilter, material,
                volumeConfig.fadeFireflies
                    ? (int)PostFXStack.Pass.BloomPrefilterFireflies
                    : (int)PostFXStack.Pass.BloomPrefilter);

            // ---- Downsample ----
            int dstPyramidIndex = 1;
            int srcPyramidIndex = 1;
            RTHandle srcHandle = prefilter;
            int i;
            for (i = 0; i < stepCount; i++)
            {
                PostFXUtility.Draw(cmd, srcHandle, pyramid[dstPyramidIndex - 1], material,
                    (int)PostFXStack.Pass.BloomHorizontal);
                PostFXUtility.Draw(cmd, pyramid[dstPyramidIndex - 1], pyramid[dstPyramidIndex], material,
                    (int)PostFXStack.Pass.BloomVertical);
                srcPyramidIndex = dstPyramidIndex;
                srcHandle = pyramid[srcPyramidIndex];
                dstPyramidIndex += 2;
            }

            // ---- Upsample ----
            cmd.SetGlobalFloat(bloomBicubicUpsamplingID, volumeConfig.bicubicUpsampling ? 1f : 0f);
            int combinePass, finalPass;
            float finalScale;
            if (volumeConfig.mode == BloomVolumeConfig.Mode.Additive)
            {
                combinePass = (int)PostFXStack.Pass.BloomAdditive;
                finalPass = (int)PostFXStack.Pass.BloomAdditiveFinal;
                finalScale = volumeConfig.intensity;
            }
            else
            {
                combinePass = (int)PostFXStack.Pass.BloomScatter;
                finalPass = (int)PostFXStack.Pass.BloomScatterFinal;
                cmd.SetGlobalFloat(bloomScatterID, volumeConfig.scatter);
                finalScale = volumeConfig.scatter;
            }

            dstPyramidIndex -= 5;
            for (i -= 1; i > 0; i--)
            {
                cmd.SetGlobalTexture(fxSource2Id, pyramid[dstPyramidIndex + 1]);
                PostFXUtility.Draw(cmd, pyramid[srcPyramidIndex], pyramid[dstPyramidIndex], material, combinePass);
                srcPyramidIndex = dstPyramidIndex;
                dstPyramidIndex -= 2;
            }

            // Final combine: blend bloom result with original source → destination
            cmd.SetGlobalTexture(fxSource2Id, source);
            cmd.SetGlobalFloat(bloomScaleID, finalScale);
            PostFXUtility.Draw(cmd, pyramid[srcPyramidIndex], destination, material, finalPass);
        }

        private static Vector4 GetKneeCurveData(BloomVolumeConfig bloomSettings)
        {
            Vector4 thresholdData;
            thresholdData.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
            thresholdData.y = thresholdData.x * bloomSettings.thresholdKnee;
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
