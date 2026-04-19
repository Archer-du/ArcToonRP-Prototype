using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// Color Grading post-processor.
    /// Ported from the old ColorGradingPass — rendering logic is identical.
    /// The color LUT is privately owned.
    /// Uses its own dedicated shader: Hidden/ArcToon/PostProcess/ColorGrading.
    /// </summary>
    public class ColorGradingProcessor : VolumePostProcessor<ColorGradingVolumeConfig>
    {
        public ColorGradingProcessor(ColorGradingVolumeConfig config) : base(config) { }

        // ---- Local pass indices (must match ColorGrading.shader pass order) ----
        private enum Pass
        {
            ColorGradingOnly,
            ColorGradingReinhard,
            ColorGradingNeutral,
            ColorGradingACES,
            ColorGradingApply,
        }

        // ---- Internal resources (self-owned) ----
        private RTHandle colorLUT;

        // ---- Cached state per frame ----
        private bool useHDR;
        private int colorLUTResolution;

        public override bool IsActive(CameraRenderer renderer)
        {
            return volumeConfig.enabled;
        }

        protected override string ShaderPath => InternalShader.Path.PostProcessColorGrading;

        public override void Setup(CameraRenderer renderer)
        {
            base.Setup(renderer);
            useHDR = renderer.useHDR;
            colorLUTResolution = (int)volumeConfig.colorLUTResolution;

            // Allocate LUT
            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            var hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            var desc = new RenderTextureDescriptor(lutWidth, lutHeight, hdrFormat, 0);
            RenderingUtils.ReAllocateIfNeeded(ref colorLUT, desc, name: "Color LUT");
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            // Configure all color grading parameters
            ConfigureColorAdjustments(cmd);
            ConfigureWhiteBalance(cmd);
            ConfigureSplitToning(cmd);
            ConfigureChannelMixer(cmd);
            ConfigureShadowsMidtonesHighlights(cmd);

            // Bake LUT
            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            cmd.SetGlobalVector(InternalShader.PropertyID.ColorGradingLUTParameters,
                new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f))
            );

            // Determine tone mapping pass
            // Local Pass enum: ColorGradingOnly=0, Reinhard=1, Neutral=2, ACES=3, Apply=4
            int toneMappingPass = (int)Pass.ColorGradingOnly + (int)volumeConfig.toneMapping;
            cmd.SetGlobalFloat(
                InternalShader.PropertyID.ColorGradingLUTInLogC,
                useHDR && toneMappingPass != (int)Pass.ColorGradingOnly ? 1f : 0f
            );

            BlitUtils.BlitTexture(cmd, source, colorLUT, material, toneMappingPass);

            // Apply LUT
            cmd.SetGlobalVector(InternalShader.PropertyID.ColorGradingLUTParameters,
                new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f)
            );
            cmd.SetGlobalTexture(InternalShader.PropertyID.ColorGradingLUT, colorLUT);
            BlitUtils.BlitTexture(cmd, source, destination, material, (int)Pass.ColorGradingApply);
        }

        private void ConfigureColorAdjustments(CommandBuffer cmd)
        {
            cmd.SetGlobalVector(InternalShader.PropertyID.ColorAdjustmentData, new Vector4(
                Mathf.Pow(2f, volumeConfig.postExposure),
                volumeConfig.contrast * 0.01f + 1f,
                volumeConfig.hueShift * (1f / 360f),
                volumeConfig.saturation * 0.01f + 1f
            ));
            cmd.SetGlobalColor(InternalShader.PropertyID.ColorFilter, volumeConfig.colorFilter.linear);
        }

        private void ConfigureWhiteBalance(CommandBuffer cmd)
        {
            cmd.SetGlobalVector(InternalShader.PropertyID.WhiteBalance, ColorUtils.ColorBalanceToLMSCoeffs(
                volumeConfig.temperature, volumeConfig.tint
            ));
        }

        private void ConfigureSplitToning(CommandBuffer cmd)
        {
            Color splitColor = volumeConfig.splitToningShadows;
            splitColor.a = volumeConfig.splitToningBalance * 0.01f;
            cmd.SetGlobalColor(InternalShader.PropertyID.SplitToningShadows, splitColor);
            cmd.SetGlobalColor(InternalShader.PropertyID.SplitToningHighlights, volumeConfig.splitToningHighlights);
        }

        private void ConfigureChannelMixer(CommandBuffer cmd)
        {
            cmd.SetGlobalVector(InternalShader.PropertyID.ChannelMixerRed, volumeConfig.channelMixerRed);
            cmd.SetGlobalVector(InternalShader.PropertyID.ChannelMixerGreen, volumeConfig.channelMixerGreen);
            cmd.SetGlobalVector(InternalShader.PropertyID.ChannelMixerBlue, volumeConfig.channelMixerBlue);
        }

        private void ConfigureShadowsMidtonesHighlights(CommandBuffer cmd)
        {
            cmd.SetGlobalColor(InternalShader.PropertyID.SMHShadows, volumeConfig.smhShadows.linear);
            cmd.SetGlobalColor(InternalShader.PropertyID.SMHMidtones, volumeConfig.smhMidtones.linear);
            cmd.SetGlobalColor(InternalShader.PropertyID.SMHHighlights, volumeConfig.smhHighlights.linear);
            cmd.SetGlobalVector(InternalShader.PropertyID.SMHRange, new Vector4(
                volumeConfig.smhShadowsStart, volumeConfig.smhShadowsEnd,
                volumeConfig.smhHighlightsStart, volumeConfig.smhHighlightsEnd
            ));
        }

        public override void Dispose()
        {
            colorLUT?.Release();
        }
    }
}
