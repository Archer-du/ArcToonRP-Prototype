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
    public class ColorGradingPass
    {
        static readonly ProfilingSampler sampler = new("Color Grading");
        
        static readonly GraphicsFormat colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

        private TextureHandle colorLUT;

        private TextureHandle source;
        private TextureHandle colorGradingResult;
        
        private int colorLUTResolution;

        private PostFXStack stack;

        private static readonly int colorGradingLUTID = Shader.PropertyToID("_ColorGradingLUT");
        private static readonly int colorGradingLUTParametersID = Shader.PropertyToID("_ColorGradingLUTParameters");
        private static readonly int colorGradingLUTInLogCID = Shader.PropertyToID("_ColorGradingLUTInLogC");


        private static readonly int colorAdjustmentDataID = Shader.PropertyToID("_ColorAdjustmentData");
        private static readonly int colorFilterID = Shader.PropertyToID("_ColorFilter");

        private static readonly int whiteBalanceID = Shader.PropertyToID("_WhiteBalance");

        private static readonly int splitToningShadowsID = Shader.PropertyToID("_SplitToningShadows");
        private static readonly int splitToningHighlightsID = Shader.PropertyToID("_SplitToningHighlights");

        private static readonly int channelMixerRedID = Shader.PropertyToID("_ChannelMixerRed");
        private static readonly int channelMixerGreenID = Shader.PropertyToID("_ChannelMixerGreen");
        private static readonly int channelMixerBlueID = Shader.PropertyToID("_ChannelMixerBlue");

        private static readonly int smhShadowsID = Shader.PropertyToID("_SMHShadows");
        private static readonly int smhMidtonesID = Shader.PropertyToID("_SMHMidtones");
        private static readonly int smhHighlightsID = Shader.PropertyToID("_SMHHighlights");
        private static readonly int smhRangeID = Shader.PropertyToID("_SMHRange");

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;

            PostFXSettings settings = stack.settings;

            ConfigureColorAdjustments(commandBuffer, settings);
            ConfigureWhiteBalance(commandBuffer, settings);
            ConfigureSplitToning(commandBuffer, settings);
            ConfigureChannelMixer(commandBuffer, settings);
            ConfigureShadowsMidtonesHighlights(commandBuffer, settings);
            
            if (stack.fxaaConfig.keepAlpha)
            {
                commandBuffer.DisableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }
            else
            {
                commandBuffer.EnableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }

            // render LUT
            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            commandBuffer.SetGlobalVector(colorGradingLUTParametersID,
                new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f))
            );
            var mode = settings.ToneMapping.mode;
            Pass pass = Pass.ColorGradingOnly + (int)mode;
            commandBuffer.SetGlobalFloat(
                colorGradingLUTInLogCID, stack.useHDR && pass != Pass.ColorGradingOnly ? 1f : 0f
            );

            stack.Draw(commandBuffer, source, colorLUT, pass);

            commandBuffer.SetGlobalVector(colorGradingLUTParametersID,
                new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f)
            );
            commandBuffer.SetGlobalTexture(colorGradingLUTID, colorLUT);
            stack.Draw(commandBuffer,source, colorGradingResult, Pass.ColorGradingApply);
            
            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public static TextureHandle Record(
            RenderGraph renderGraph,
            PostFXStack stack,
            int colorLUTResolution,
            in TextureHandle srcHandle)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out ColorGradingPass pass, sampler);
            
            pass.stack = stack;
            pass.colorLUTResolution = colorLUTResolution;
            pass.source = builder.ReadTexture(srcHandle);
            
            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            var desc = new TextureDesc(lutWidth, lutHeight)
            {
                colorFormat = colorFormat,
                name = "Color LUT"
            };
            pass.colorLUT = builder.CreateTransientTexture(desc);
            desc = new TextureDesc(stack.bufferSize.x, stack.bufferSize.y)
            {
                colorFormat = colorFormat,
                name = "Color Grading"
            };
            pass.colorGradingResult = builder.WriteTexture(renderGraph.CreateTexture(desc));
            
            builder.SetRenderFunc<ColorGradingPass>(static (pass, context) => pass.Render(context));
            
            return pass.colorGradingResult;
        }

        void ConfigureColorAdjustments(CommandBuffer commandBuffer, PostFXSettings settings)
        {
            ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
            commandBuffer.SetGlobalVector(colorAdjustmentDataID, new Vector4(
                Mathf.Pow(2f, colorAdjustments.postExposure),
                colorAdjustments.contrast * 0.01f + 1f,
                colorAdjustments.hueShift * (1f / 360f),
                colorAdjustments.saturation * 0.01f + 1f
            ));
            commandBuffer.SetGlobalColor(colorFilterID, colorAdjustments.colorFilter.linear);
        }

        void ConfigureWhiteBalance(CommandBuffer commandBuffer, PostFXSettings settings)
        {
            WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
            commandBuffer.SetGlobalVector(whiteBalanceID, ColorUtils.ColorBalanceToLMSCoeffs(
                whiteBalance.temperature, whiteBalance.tint
            ));
        }

        void ConfigureSplitToning(CommandBuffer commandBuffer, PostFXSettings settings)
        {
            SplitToningSettings splitToning = settings.SplitToning;
            Color splitColor = splitToning.shadows;
            splitColor.a = splitToning.balance * 0.01f;
            commandBuffer.SetGlobalColor(splitToningShadowsID, splitColor);
            commandBuffer.SetGlobalColor(splitToningHighlightsID, splitToning.highlights);
        }

        void ConfigureChannelMixer(CommandBuffer commandBuffer, PostFXSettings settings)
        {
            ChannelMixerSettings channelMixer = settings.ChannelMixer;
            commandBuffer.SetGlobalVector(channelMixerRedID, channelMixer.red);
            commandBuffer.SetGlobalVector(channelMixerGreenID, channelMixer.green);
            commandBuffer.SetGlobalVector(channelMixerBlueID, channelMixer.blue);
        }

        void ConfigureShadowsMidtonesHighlights(CommandBuffer commandBuffer, PostFXSettings settings)
        {
            ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
            commandBuffer.SetGlobalColor(smhShadowsID, smh.shadows.linear);
            commandBuffer.SetGlobalColor(smhMidtonesID, smh.midtones.linear);
            commandBuffer.SetGlobalColor(smhHighlightsID, smh.highlights.linear);
            commandBuffer.SetGlobalVector(smhRangeID, new Vector4(
                smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
            ));
        }
    }
}