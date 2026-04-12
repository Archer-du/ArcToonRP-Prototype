using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing
{
    /// <summary>
    /// FXAA anti-aliasing post-processor.
    /// Ported from the old AntiAliasingPass — rendering logic is identical.
    /// </summary>
    public class FXAAProcessor : PostProcessor
    {
        public override string Name => "FXAA";
        public override int Order => 900;

        // ---- Cached state per frame ----
    private FXAAVolumeConfig settings;
        private Material material;
        private bool keepAlpha;

        // ---- Shader property IDs & keywords ----
        private static readonly int fxaaParamsID = Shader.PropertyToID("_FXAAParams");

        private static readonly GlobalKeyword fxaaQualityLowKeyword = GlobalKeyword.Create("FXAA_QUALITY_LOW");
        private static readonly GlobalKeyword fxaaQualityMediumKeyword = GlobalKeyword.Create("FXAA_QUALITY_MEDIUM");

        public override bool IsActive(PostProcessConfig config, CameraRenderer renderer)
        {
        var s = config.GetVolumeConfig<FXAAVolumeConfig>();
            return s != null && s.enabled && renderer.CameraAdditiveData.allowFXAA;
        }

        public override void Setup(PostProcessConfig config, CameraRenderer renderer)
        {
        settings = config.GetVolumeConfig<FXAAVolumeConfig>();
            // Use explicit Unity null check — ??= won't catch destroyed-but-not-null objects
            if (material == null)
                material = ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.PostFXStack);
            keepAlpha = renderer.CameraAdditiveData.keepAlpha;
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            ConfigureFXAA(cmd);
            PostFXUtility.Draw(cmd, source, destination, material, (int)PostFXStack.Pass.FXAA);
        }

        private void ConfigureFXAA(CommandBuffer cmd)
        {
            // Quality keywords
        if (settings.quality == FXAAVolumeConfig.Quality.Low)
            {
                cmd.SetKeyword(fxaaQualityLowKeyword, true);
                cmd.SetKeyword(fxaaQualityMediumKeyword, false);
            }
        else if (settings.quality == FXAAVolumeConfig.Quality.Medium)
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
                settings.fixedThreshold,
                settings.relativeThreshold,
                settings.subpixelBlending
            ));
        }
    }
}
