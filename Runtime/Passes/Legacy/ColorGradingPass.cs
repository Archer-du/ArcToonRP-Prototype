using ArcToon.Data;
using ArcToon.Settings;
using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using static ArcToon.Passes.Legacy.PostFXConfig;
using static ArcToon.Utils.PostFXStack;

namespace ArcToon.Passes.Legacy
{
    public class ColorGradingPass
    {
        private PostFXStack stack;

        private PostFXConfig config;
        private bool useHDR;
        
        private int colorLUTResolution;

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

        /// <summary>
        /// Execute color grading pass. Reads from sourceHandle, writes to resources.PostFX.colorGradingResult.
        /// </summary>
        public void Execute(CommandBuffer cmd, RenderResources resources, CameraRenderer renderer,
            PostFXConfig postFXConfig, PostFXStack stack,
            RTHandle sourceHandle)
        {
            this.stack = stack;
            this.config = postFXConfig;
            this.useHDR = renderer.useHDR;
            this.colorLUTResolution = postFXConfig ? (int)postFXConfig.ToneMapping.colorLUTResolution : 0;

            // Allocate LUT
            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            resources.PostFX.AllocateColorLUT(lutWidth, lutHeight);

            Render(cmd, resources, sourceHandle);
        }

        private void Render(CommandBuffer commandBuffer, RenderResources resources, RTHandle source)
        {
            ConfigureColorAdjustments(commandBuffer, config);
            ConfigureWhiteBalance(commandBuffer, config);
            ConfigureSplitToning(commandBuffer, config);
            ConfigureChannelMixer(commandBuffer, config);
            ConfigureShadowsMidtonesHighlights(commandBuffer, config);
            
            // render LUT
            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            commandBuffer.SetGlobalVector(colorGradingLUTParametersID,
                new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f))
            );
            var mode = config.ToneMapping.mode;
            Pass pass = Pass.ColorGradingOnly + (int)mode;
            commandBuffer.SetGlobalFloat(
                colorGradingLUTInLogCID, useHDR && pass != Pass.ColorGradingOnly ? 1f : 0f
            );

            stack.Draw(commandBuffer, source, resources.PostFX.colorLUT, pass);

            commandBuffer.SetGlobalVector(colorGradingLUTParametersID,
                new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f)
            );
            commandBuffer.SetGlobalTexture(colorGradingLUTID, resources.PostFX.colorLUT);
            stack.Draw(commandBuffer, source, resources.PostFX.colorGradingResult, Pass.ColorGradingApply);
        }

        void ConfigureColorAdjustments(CommandBuffer commandBuffer, PostFXConfig config)
        {
            ColorAdjustmentsSettings colorAdjustments = config.ColorAdjustments;
            commandBuffer.SetGlobalVector(colorAdjustmentDataID, new Vector4(
                Mathf.Pow(2f, colorAdjustments.postExposure),
                colorAdjustments.contrast * 0.01f + 1f,
                colorAdjustments.hueShift * (1f / 360f),
                colorAdjustments.saturation * 0.01f + 1f
            ));
            commandBuffer.SetGlobalColor(colorFilterID, colorAdjustments.colorFilter.linear);
        }

        void ConfigureWhiteBalance(CommandBuffer commandBuffer, PostFXConfig config)
        {
            WhiteBalanceSettings whiteBalance = config.WhiteBalance;
            commandBuffer.SetGlobalVector(whiteBalanceID, ColorUtils.ColorBalanceToLMSCoeffs(
                whiteBalance.temperature, whiteBalance.tint
            ));
        }

        void ConfigureSplitToning(CommandBuffer commandBuffer, PostFXConfig config)
        {
            SplitToningSettings splitToning = config.SplitToning;
            Color splitColor = splitToning.shadows;
            splitColor.a = splitToning.balance * 0.01f;
            commandBuffer.SetGlobalColor(splitToningShadowsID, splitColor);
            commandBuffer.SetGlobalColor(splitToningHighlightsID, splitToning.highlights);
        }

        void ConfigureChannelMixer(CommandBuffer commandBuffer, PostFXConfig config)
        {
            ChannelMixerSettings channelMixer = config.ChannelMixer;
            commandBuffer.SetGlobalVector(channelMixerRedID, channelMixer.red);
            commandBuffer.SetGlobalVector(channelMixerGreenID, channelMixer.green);
            commandBuffer.SetGlobalVector(channelMixerBlueID, channelMixer.blue);
        }

        void ConfigureShadowsMidtonesHighlights(CommandBuffer commandBuffer, PostFXConfig config)
        {
            ShadowsMidtonesHighlightsSettings smh = config.ShadowsMidtonesHighlights;
            commandBuffer.SetGlobalColor(smhShadowsID, smh.shadows.linear);
            commandBuffer.SetGlobalColor(smhMidtonesID, smh.midtones.linear);
            commandBuffer.SetGlobalColor(smhHighlightsID, smh.highlights.linear);
            commandBuffer.SetGlobalVector(smhRangeID, new Vector4(
                smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
            ));
        }
    }
}