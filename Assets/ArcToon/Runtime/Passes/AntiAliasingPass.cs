using ArcToon.Runtime.Data;
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
    public class AntiAliasingPass
    {
        static readonly ProfilingSampler sampler = new("FXAA");

        private TextureHandle source;
        private TextureHandle result;

        private PostFXStack stack;

        private FXAARuntimeConfig fxaaConfig;
        
        private static readonly int fxaaParamsID = Shader.PropertyToID("_FXAAParams");
        private static Vector4 fxaaParams;

        static readonly GlobalKeyword
            fxaaQualityLowKeyword = GlobalKeyword.Create("FXAA_QUALITY_LOW"),
            fxaaQualityMediumKeyword = GlobalKeyword.Create("FXAA_QUALITY_MEDIUM");
        
        public struct FXAARuntimeConfig
        {
            public bool enabled;
            public bool keepAlpha;
            public float fixedThreshold;
            public float relativeThreshold;
            public float subpixelBlending;
            public CameraBufferSettings.FXAASettings.Quality quality;
        }

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;

            ConfigureFXAA(commandBuffer);
            stack.Draw(commandBuffer, source, result, Pass.FXAA);

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
            // TODO: buffer settings translate
            FXAARuntimeConfig fxaaConfig = new FXAARuntimeConfig
            {
                enabled = bufferSettings.fxaaSettings.enabled && cameraSettings.allowFXAA,
                keepAlpha = cameraSettings.keepAlpha,
                fixedThreshold = bufferSettings.fxaaSettings.fixedThreshold,
                relativeThreshold = bufferSettings.fxaaSettings.relativeThreshold,
                subpixelBlending = bufferSettings.fxaaSettings.subpixelBlending,
                quality = bufferSettings.fxaaSettings.quality,
            };
            
            if (!fxaaConfig.enabled) return srcHandle;

            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out AntiAliasingPass pass, sampler);

            pass.stack = stack;
            pass.fxaaConfig = fxaaConfig;
            pass.source = builder.ReadTexture(srcHandle);

            var desc = new TextureDesc(bufferSize.x, bufferSize.y)
            {
                colorFormat = SystemInfo.GetGraphicsFormat(
                    useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                name = "FXAA Result"
            };

            pass.result = builder.WriteTexture(renderGraph.CreateTexture(desc));

            builder.SetRenderFunc<AntiAliasingPass>(
                static (pass, context) => pass.Render(context));

            return pass.result;
        }

        void ConfigureFXAA(CommandBuffer commandBuffer)
        {
            if (fxaaConfig.quality == CameraBufferSettings.FXAASettings.Quality.Low)
            {
                commandBuffer.SetKeyword(fxaaQualityLowKeyword, true);
                commandBuffer.SetKeyword(fxaaQualityMediumKeyword, false);
            }
            else if (fxaaConfig.quality == CameraBufferSettings.FXAASettings.Quality.Medium)
            {
                commandBuffer.SetKeyword(fxaaQualityLowKeyword, false);
                commandBuffer.SetKeyword(fxaaQualityMediumKeyword, true);
            }
            else
            {
                commandBuffer.SetKeyword(fxaaQualityLowKeyword, false);
                commandBuffer.SetKeyword(fxaaQualityMediumKeyword, false);
            }

            if (fxaaConfig.keepAlpha)
            {
                commandBuffer.DisableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }
            else
            {
                commandBuffer.EnableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }

            fxaaParams = new Vector4(fxaaConfig.fixedThreshold, fxaaConfig.relativeThreshold,
                fxaaConfig.subpixelBlending);
            commandBuffer.SetGlobalVector(fxaaParamsID, fxaaParams);
        }
    }
}