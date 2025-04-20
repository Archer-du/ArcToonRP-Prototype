using ArcToon.Runtime.Data;
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

        private static readonly int fxaaParamsID = Shader.PropertyToID("_FXAAParams");
        private static Vector4 fxaaParams;

        private TextureHandle source;
        private TextureHandle result;

        private PostFXStack stack;

        static readonly GlobalKeyword
            fxaaQualityLowKeyword = GlobalKeyword.Create("FXAA_QUALITY_LOW"),
            fxaaQualityMediumKeyword = GlobalKeyword.Create("FXAA_QUALITY_MEDIUM");

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;

            ConfigureFXAA(commandBuffer);
            stack.Draw(commandBuffer, source, result, Pass.FXAA);

            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public static TextureHandle Record(
            RenderGraph renderGraph,
            PostFXStack stack,
            in TextureHandle srcHandle)
        {
            if (!stack.fxaaConfig.enabled) return srcHandle;

            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out AntiAliasingPass pass, sampler);

            pass.stack = stack;
            pass.source = builder.ReadTexture(srcHandle);

            var desc = new TextureDesc(stack.bufferSize.x, stack.bufferSize.y)
            {
                colorFormat = SystemInfo.GetGraphicsFormat(
                    stack.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                name = "FXAA Result"
            };

            pass.result = builder.WriteTexture(renderGraph.CreateTexture(desc));

            builder.SetRenderFunc<AntiAliasingPass>(
                static (pass, context) => pass.Render(context));

            return pass.result;
        }

        void ConfigureFXAA(CommandBuffer commandBuffer)
        {
            if (stack.fxaaConfig.quality == CameraBufferSettings.FXAASettings.Quality.Low)
            {
                commandBuffer.SetKeyword(fxaaQualityLowKeyword, true);
                commandBuffer.SetKeyword(fxaaQualityMediumKeyword, false);
            }
            else if (stack.fxaaConfig.quality == CameraBufferSettings.FXAASettings.Quality.Medium)
            {
                commandBuffer.SetKeyword(fxaaQualityLowKeyword, false);
                commandBuffer.SetKeyword(fxaaQualityMediumKeyword, true);
            }
            else
            {
                commandBuffer.SetKeyword(fxaaQualityLowKeyword, false);
                commandBuffer.SetKeyword(fxaaQualityMediumKeyword, false);
            }

            if (stack.fxaaConfig.keepAlpha)
            {
                commandBuffer.DisableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }
            else
            {
                commandBuffer.EnableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }

            fxaaParams = new Vector4(stack.fxaaConfig.fixedThreshold, stack.fxaaConfig.relativeThreshold,
                stack.fxaaConfig.subpixelBlending);
            commandBuffer.SetGlobalVector(fxaaParamsID, fxaaParams);
        }
    }
}