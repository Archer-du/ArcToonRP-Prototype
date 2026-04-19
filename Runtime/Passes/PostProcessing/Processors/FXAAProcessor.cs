using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// FXAA anti-aliasing post-processor.
    /// Ported from the old AntiAliasingPass — rendering logic is identical.
    /// Uses its own dedicated shader: Hidden/ArcToon/PostProcess/FXAA.
    /// </summary>
    public class FXAAProcessor : VolumePostProcessor<FXAAVolumeConfig>
    {
        public FXAAProcessor(FXAAVolumeConfig config) : base(config) { }

        // ---- Local pass indices (must match FXAA.shader pass order) ----
        private const int FXAAPass = 0;

        // ---- Cached state per frame ----
        private bool keepAlpha;

        // ---- Shader property IDs & keywords ----
        private static readonly int fxaaParamsID = Shader.PropertyToID("_FXAAParams");

        private static readonly GlobalKeyword fxaaQualityLowKeyword = GlobalKeyword.Create("FXAA_QUALITY_LOW");
        private static readonly GlobalKeyword fxaaQualityMediumKeyword = GlobalKeyword.Create("FXAA_QUALITY_MEDIUM");

        public override bool IsActive(CameraRenderer renderer)
        {
            return volumeConfig.enabled && renderer.CameraAdditiveData.allowFXAA;
        }

        protected override string ShaderPath => InternalShader.Path.PostProcessFXAA;

        public override void Setup(CameraRenderer renderer)
        {
            base.Setup(renderer);
            keepAlpha = renderer.CameraAdditiveData.keepAlpha;
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            ConfigureFXAA(cmd);
            BlitUtils.BlitTexture(cmd, source, destination, material, FXAAPass);
        }

        private void ConfigureFXAA(CommandBuffer cmd)
        {
            // Quality keywords
            if (volumeConfig.quality == FXAAVolumeConfig.Quality.Low)
            {
                cmd.SetKeyword(fxaaQualityLowKeyword, true);
                cmd.SetKeyword(fxaaQualityMediumKeyword, false);
            }
            else if (volumeConfig.quality == FXAAVolumeConfig.Quality.Medium)
            {
                cmd.SetKeyword(fxaaQualityLowKeyword, false);
                cmd.SetKeyword(fxaaQualityMediumKeyword, true);
            }
            else
            {
                cmd.SetKeyword(fxaaQualityLowKeyword, false);
                cmd.SetKeyword(fxaaQualityMediumKeyword, false);
            }

            // Alpha handling
            if (keepAlpha)
            {
                cmd.DisableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }
            else
            {
                cmd.EnableShaderKeyword("FXAA_ALPHA_CONTAINS_LUMA");
            }

            // FXAA parameters
            cmd.SetGlobalVector(fxaaParamsID, new Vector4(
                volumeConfig.fixedThreshold,
                volumeConfig.relativeThreshold,
                volumeConfig.subpixelBlending
            ));
        }
    }
}
