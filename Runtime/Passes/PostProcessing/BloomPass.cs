using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static ArcToon.Runtime.Settings.PostFXConfig;
using static ArcToon.Runtime.PostFXStack;

namespace ArcToon.Runtime.Passes.PostProcessing
{
    public class BloomPass
    {
        private PostFXStack stack;
        
        private BloomSettings bloomSettings;
        
        private static readonly int fxSource2Id = Shader.PropertyToID("_PostFXSource2");

        private int stepCount;

        private static readonly int bloomThresholdID = Shader.PropertyToID("_BloomThreshold");
        private static readonly int bloomBucibicUpsamplingID = Shader.PropertyToID("_BloomBicubicUpsampling");
        private static readonly int bloomScaleID = Shader.PropertyToID("_BloomScale");
        private static readonly int bloomScatterID = Shader.PropertyToID("_BloomScatter");

        /// <summary>
        /// Setup and execute bloom pass. Returns true if bloom was applied.
        /// If bloom is skipped, postFXResult is not modified.
        /// </summary>
        public bool Execute(CommandBuffer cmd, RenderResources resources, CameraRenderer renderer,
            PostFXConfig postFXConfig, PostFXStack stack)
        {
            this.stack = stack;
            bloomSettings = postFXConfig.Bloom;

            Vector2Int bufferSize = renderer.AttachmentSize;
            Vector2Int originalBufferSize = bufferSize;
            bufferSize = bloomSettings.ignoreRenderScale
                ? new Vector2Int(renderer.RenderCamera.pixelWidth, renderer.RenderCamera.pixelHeight)
                : bufferSize;

            if (bloomSettings.maxIterations == 0 ||
                bloomSettings.intensity <= 0f ||
                bufferSize.y < bloomSettings.downscaleLimit * 4 ||
                bufferSize.x < bloomSettings.downscaleLimit * 4)
            {
                return false;
            }

            // Allocate pyramid levels
            bufferSize /= 2;
            // bloomPrefilter is already allocated in RenderResources.AllocatePostFXResources
            // but we need to re-check size for ignoreRenderScale case
            var colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            {
                var prefilterDesc = new RenderTextureDescriptor(bufferSize.x, bufferSize.y, colorFormat, 0);
                RenderResources.ReAllocateHandleIfNeeded(ref resources.bloomPrefilter, prefilterDesc, name: "Bloom Prefilter");
            }

            bufferSize /= 2;
            int pyramidIndex = 0;
            int i;
            for (i = 0; i < bloomSettings.maxIterations; i++, pyramidIndex += 2)
            {
                if (bufferSize.y < bloomSettings.downscaleLimit || bufferSize.x < bloomSettings.downscaleLimit)
                {
                    break;
                }

                resources.AllocateBloomPyramidLevel(pyramidIndex, bufferSize.x, bufferSize.y, renderer.useHDR, "Bloom Pyramid H");
                resources.AllocateBloomPyramidLevel(pyramidIndex + 1, bufferSize.x, bufferSize.y, renderer.useHDR, "Bloom Pyramid V");
                bufferSize /= 2;
            }

            stepCount = i;

            // Allocate bloom result at original buffer size
            {
                var resultDesc = new RenderTextureDescriptor(originalBufferSize.x, originalBufferSize.y, colorFormat, 0);
                RenderResources.ReAllocateHandleIfNeeded(ref resources.bloomResult, resultDesc, name: "Bloom Result");
            }

            // Render
            Render(cmd, resources);
            return true;
        }

        private void Render(CommandBuffer commandBuffer, RenderResources resources)
        {
            commandBuffer.SetGlobalVector(bloomThresholdID, GetKneeCurveData(bloomSettings));

            // knee curve prefilter
            stack.Draw(commandBuffer, resources.colorAttachment, resources.bloomPrefilter,
                bloomSettings.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);

            // down sample
            int dstPyramidIndex = 1;
            int srcPyramidIndex = 1;
            RTHandle srcHandle = resources.bloomPrefilter;
            int i;
            for (i = 0; i < stepCount; i++)
            {
                stack.Draw(commandBuffer, srcHandle, resources.bloomPyramid[dstPyramidIndex - 1],
                    Pass.BloomHorizontal);
                stack.Draw(commandBuffer, resources.bloomPyramid[dstPyramidIndex - 1], resources.bloomPyramid[dstPyramidIndex],
                    Pass.BloomVertical);
                srcPyramidIndex = dstPyramidIndex;
                srcHandle = resources.bloomPyramid[srcPyramidIndex];
                dstPyramidIndex += 2;
            }

            // up sample
            commandBuffer.SetGlobalFloat(bloomBucibicUpsamplingID, bloomSettings.bicubicUpsampling ? 1f : 0f);
            Pass combinePass, finalPass;
            float finalScale;
            if (bloomSettings.mode == BloomSettings.Mode.Additive)
            {
                combinePass = Pass.BloomAdditive;
                finalPass = Pass.BloomAdditiveFinal;
                finalScale = bloomSettings.intensity;
            }
            else
            {
                combinePass = Pass.BloomScatter;
                finalPass = Pass.BloomScatterFinal;
                commandBuffer.SetGlobalFloat(bloomScatterID, bloomSettings.scatter);
                finalScale = bloomSettings.scatter;
            }

            dstPyramidIndex -= 5;
            for (i -= 1; i > 0; i--)
            {
                commandBuffer.SetGlobalTexture(fxSource2Id, resources.bloomPyramid[dstPyramidIndex + 1]);
                stack.Draw(commandBuffer, resources.bloomPyramid[srcPyramidIndex], resources.bloomPyramid[dstPyramidIndex], combinePass);
                srcPyramidIndex = dstPyramidIndex;
                dstPyramidIndex -= 2;
            }

            commandBuffer.SetGlobalTexture(fxSource2Id, resources.colorAttachment);
            commandBuffer.SetGlobalFloat(bloomScaleID, finalScale);
            stack.Draw(commandBuffer, resources.bloomPyramid[srcPyramidIndex], resources.bloomResult, finalPass);
        }

        private Vector4 GetKneeCurveData(BloomSettings bloomSettings)
        {
            Vector4 thresholdData;
            thresholdData.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
            thresholdData.y = thresholdData.x * bloomSettings.thresholdKnee;
            thresholdData.z = 2f * thresholdData.y;
            thresholdData.w = 1f / (4 * thresholdData.y + 0.00001f);
            thresholdData.y -= thresholdData.x;
            return thresholdData;
        }
    }
}