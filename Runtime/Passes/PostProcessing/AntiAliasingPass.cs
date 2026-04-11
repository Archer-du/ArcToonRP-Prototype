using ArcToon.Data;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using static ArcToon.Runtime.PostFXStack;

namespace ArcToon.Runtime.Passes.PostProcessing
{
    public class AntiAliasingPass
    {
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

        /// <summary>
        /// Execute FXAA pass. Returns true if FXAA was applied.
        /// Reads from sourceHandle, writes to resources.PostFX.fxaaResult.
        /// </summary>
        public bool Execute(CommandBuffer cmd, RenderResources resources, CameraRenderer renderer,
            PostFXStack stack,
            RTHandle sourceHandle)
        {
            // TODO: buffer settings translate
            fxaaConfig = new FXAARuntimeConfig
            {
                enabled = renderer.BufferSettings.fxaaSettings.enabled && renderer.CameraAdditiveData.allowFXAA,
                keepAlpha = renderer.CameraAdditiveData.keepAlpha,
                fixedThreshold = renderer.BufferSettings.fxaaSettings.fixedThreshold,
                relativeThreshold = renderer.BufferSettings.fxaaSettings.relativeThreshold,
                subpixelBlending = renderer.BufferSettings.fxaaSettings.subpixelBlending,
                quality = renderer.BufferSettings.fxaaSettings.quality,
            };
            
            if (!fxaaConfig.enabled) return false;

            this.stack = stack;

            ConfigureFXAA(cmd);
            stack.Draw(cmd, sourceHandle, resources.PostFX.fxaaResult, Pass.FXAA);
            return true;
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