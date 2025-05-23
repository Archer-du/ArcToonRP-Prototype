﻿using ArcToon.Runtime.Data;
using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Passes.PostProcess;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using static ArcToon.Runtime.Settings.PostFXSettings;
using static ArcToon.Runtime.Passes.PostProcess.PostFXStack;

namespace ArcToon.Runtime.Passes
{
    public class BloomPass
    {
        static readonly ProfilingSampler sampler = new("Bloom");

        private TextureHandle source;
        private TextureHandle result;

        private PostFXStack stack;
        
        private BloomSettings bloomSettings;

        static readonly GraphicsFormat colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
        
        private static readonly int fxSource2Id = Shader.PropertyToID("_PostFXSource2");

        private TextureHandle bloomPrefilter;
        private readonly TextureHandle[] pyramid =
            new TextureHandle[2 * maxBloomPyramidLevels];

        private int stepCount;

        const int maxBloomPyramidLevels = 16;

        // private static readonly int bloomPyramidId;
        private static readonly int bloomThresholdID = Shader.PropertyToID("_BloomThreshold");
        private static readonly int bloomBucibicUpsamplingID = Shader.PropertyToID("_BloomBicubicUpsampling");
        private static readonly int bloomScaleID = Shader.PropertyToID("_BloomScale");
        private static readonly int bloomScatterID = Shader.PropertyToID("_BloomScatter");

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;

            commandBuffer.SetGlobalVector(bloomThresholdID, GetKneeCurveData(bloomSettings));

            // knee curve prefilter
            stack.Draw(commandBuffer, source, bloomPrefilter,
                bloomSettings.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);

            // down sample
            int dstPyramidIndex = 1;
            int srcPyramidIndex = 1;
            TextureHandle srcHandle = bloomPrefilter;
            int i;
            for (i = 0; i < stepCount; i++)
            {
                stack.Draw(commandBuffer, srcHandle, pyramid[dstPyramidIndex - 1],
                    Pass.BloomHorizontal);
                stack.Draw(commandBuffer, pyramid[dstPyramidIndex - 1], pyramid[dstPyramidIndex],
                    Pass.BloomVertical);
                srcPyramidIndex = dstPyramidIndex;
                srcHandle = pyramid[srcPyramidIndex];
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
                commandBuffer.SetGlobalTexture(fxSource2Id, pyramid[dstPyramidIndex + 1]);
                stack.Draw(commandBuffer, pyramid[srcPyramidIndex], pyramid[dstPyramidIndex], combinePass);
                srcPyramidIndex = dstPyramidIndex;
                dstPyramidIndex -= 2;
            }

            commandBuffer.SetGlobalTexture(fxSource2Id, source);
            commandBuffer.SetGlobalFloat(bloomScaleID, finalScale);
            stack.Draw(commandBuffer, pyramid[srcPyramidIndex], result, finalPass);
            
            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public static TextureHandle Record(RenderGraph renderGraph, Camera camera,
            CullingResults cullingResults, Vector2Int bufferSize,
            CameraSettings cameraSettings,
            CameraBufferSettings bufferSettings,
            PostFXSettings postFXSettings,
            bool useHDR,
            in TextureHandle srcHandle, 
            PostFXStack stack)
        {
            BloomSettings bloom = postFXSettings.Bloom;
            Vector2Int originalBufferSize = bufferSize;
            bufferSize = bloom.ignoreRenderScale
                ? new Vector2Int(camera.pixelWidth, camera.pixelHeight)
                : bufferSize;

            if (bloom.maxIterations == 0 ||
                bloom.intensity <= 0f ||
                bufferSize.y < bloom.downscaleLimit * 4 ||
                bufferSize.x < bloom.downscaleLimit * 4)
            {
                return srcHandle;
            }

            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out BloomPass pass, sampler);

            pass.stack = stack;
            pass.bloomSettings = bloom;
            pass.source = builder.ReadTexture(srcHandle);

            bufferSize /= 2;
            var desc = new TextureDesc(bufferSize.x, bufferSize.y)
            {
                colorFormat = SystemInfo.GetGraphicsFormat(
                    useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                name = "Bloom Prefilter"
            };
            pass.bloomPrefilter = builder.CreateTransientTexture(desc);
            
            TextureHandle[] pyramid = pass.pyramid;
            bufferSize /= 2;
            int pyramidIndex = 0;
            int i;
            for (i = 0; i < bloom.maxIterations; i++, pyramidIndex += 2)
            {
                if (bufferSize.y < bloom.downscaleLimit || bufferSize.x < bloom.downscaleLimit)
                {
                    break;
                }

                desc.width = bufferSize.x;
                desc.height = bufferSize.y;
                desc.name = "Bloom Pyramid H";
                pyramid[pyramidIndex] = builder.CreateTransientTexture(desc);
                desc.name = "Bloom Pyramid V";
                pyramid[pyramidIndex + 1] = builder.CreateTransientTexture(desc);
                bufferSize /= 2;
            }

            pass.stepCount = i;

            desc.width = originalBufferSize.x;
            desc.height = originalBufferSize.y;
            desc.name = "Bloom Result";
            pass.result = builder.WriteTexture(renderGraph.CreateTexture(desc));
            
            builder.SetRenderFunc<BloomPass>(
                static (pass, context) => pass.Render(context));
            
            return pass.result;
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