using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ArcToon.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Render Pipeline")]
    public class ArcToonRenderPipelineAsset : RenderPipelineAsset<ArcToonRenderPipelineInstance>
    {
        [SerializeField] bool enableSRPBatcher;
        [SerializeField] bool enablePostProcessing = true;

        [FormerlySerializedAs("GlobalShadowSettings")] [SerializeField]
        ShadowSettings globalShadowSettings;

        [FormerlySerializedAs("postFXSettings")] [SerializeField]
        PostFXSettings globalPostFXSettings;

        [SerializeField] private CameraBufferSettings cameraBufferSettings = new()
        {
            allowHDR = true,
            renderScale = 1f,
            fxaaSettings = new CameraBufferSettings.FXAASettings
            {
                fixedThreshold = 0.0833f,
                relativeThreshold = 0.166f,
                subpixelBlending = 0.75f,
            }
        };

        // not editable
        [SerializeField] private Shader cameraCopyShader;

        protected override RenderPipeline CreatePipeline()
        {
            return new ArcToonRenderPipelineInstance(
                globalShadowSettings,
                enablePostProcessing ? globalPostFXSettings : null,
                cameraBufferSettings,
                cameraCopyShader,
                enableSRPBatcher
            );
        }
    }
}