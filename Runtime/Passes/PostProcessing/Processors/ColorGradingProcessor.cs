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
    /// </summary>
    public class ColorGradingProcessor : VolumePostProcessor<ColorGradingVolumeConfig>
    {
        public ColorGradingProcessor(ColorGradingVolumeConfig config) : base(config) { }

        // ---- Internal resources (self-owned) ----
        private RTHandle colorLUT;

        // ---- Cached state per frame ----
        private Material material;
        private bool useHDR;
        private int colorLUTResolution;

        // ---- Shader property IDs ----
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

        public override bool IsActive(CameraRenderer renderer)
        {
            return volumeConfig.enabled;
        }

        public override void Setup(CameraRenderer renderer)
        {
            // Use explicit Unity null check — ??= won't catch destroyed-but-not-null objects
            if (material == null)
                material = ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.PostFXStack);
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
            cmd.SetGlobalVector(colorGradingLUTParametersID,
                new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f))
            );

            // Determine tone mapping pass
            // PostFXStack.Pass enum: ColorGradingOnly=8, Reinhard=9, Neutral=10, ACES=11
            int toneMappingPass = (int)PostFXStack.Pass.ColorGradingOnly + (int)volumeConfig.toneMapping;
            cmd.SetGlobalFloat(
                colorGradingLUTInLogCID,
                useHDR && toneMappingPass != (int)PostFXStack.Pass.ColorGradingOnly ? 1f : 0f
            );

            PostFXUtility.Draw(cmd, source, colorLUT, material, toneMappingPass);

            // Apply LUT
            cmd.SetGlobalVector(colorGradingLUTParametersID,
                new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f)
            );
            cmd.SetGlobalTexture(colorGradingLUTID, colorLUT);
            PostFXUtility.Draw(cmd, source, destination, material, (int)PostFXStack.Pass.ColorGradingApply);
        }

        private void ConfigureColorAdjustments(CommandBuffer cmd)
        {
            cmd.SetGlobalVector(colorAdjustmentDataID, new Vector4(
                Mathf.Pow(2f, volumeConfig.postExposure),
                volumeConfig.contrast * 0.01f + 1f,
                volumeConfig.hueShift * (1f / 360f),
                volumeConfig.saturation * 0.01f + 1f
            ));
            cmd.SetGlobalColor(colorFilterID, volumeConfig.colorFilter.linear);
        }

        private void ConfigureWhiteBalance(CommandBuffer cmd)
        {
            cmd.SetGlobalVector(whiteBalanceID, ColorUtils.ColorBalanceToLMSCoeffs(
                volumeConfig.temperature, volumeConfig.tint
            ));
        }

        private void ConfigureSplitToning(CommandBuffer cmd)
        {
            Color splitColor = volumeConfig.splitToningShadows;
            splitColor.a = volumeConfig.splitToningBalance * 0.01f;
            cmd.SetGlobalColor(splitToningShadowsID, splitColor);
            cmd.SetGlobalColor(splitToningHighlightsID, volumeConfig.splitToningHighlights);
        }

        private void ConfigureChannelMixer(CommandBuffer cmd)
        {
            cmd.SetGlobalVector(channelMixerRedID, volumeConfig.channelMixerRed);
            cmd.SetGlobalVector(channelMixerGreenID, volumeConfig.channelMixerGreen);
            cmd.SetGlobalVector(channelMixerBlueID, volumeConfig.channelMixerBlue);
        }

        private void ConfigureShadowsMidtonesHighlights(CommandBuffer cmd)
        {
            cmd.SetGlobalColor(smhShadowsID, volumeConfig.smhShadows.linear);
            cmd.SetGlobalColor(smhMidtonesID, volumeConfig.smhMidtones.linear);
            cmd.SetGlobalColor(smhHighlightsID, volumeConfig.smhHighlights.linear);
            cmd.SetGlobalVector(smhRangeID, new Vector4(
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
