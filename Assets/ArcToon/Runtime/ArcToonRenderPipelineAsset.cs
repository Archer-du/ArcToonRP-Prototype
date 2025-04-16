using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ArcToon.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Render Pipeline")]
    public partial class ArcToonRenderPipelineAsset : RenderPipelineAsset<ArcToonRenderPipelineInstance>
    {
        [SerializeField] bool enableSRPBatcher;
        [SerializeField] bool enableGPUInstancing;
        [SerializeField] bool enablePostProcessing = true;

        public enum ColorLUTResolution
        {
            _16 = 16,
            _32 = 32,
            _64 = 64
        }

        [SerializeField] ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

        [FormerlySerializedAs("GlobalShadowSettings")] [SerializeField] ShadowSettings globalShadowSettings;
        [FormerlySerializedAs("postFXSettings")] [SerializeField] PostFXSettings globalPostFXSettings;

        [SerializeField] private CameraBufferSettings cameraBufferSettings = new()
        {
            allowHDR = true,
            renderScale = 1f
        };
        // not editable
        [SerializeField] private Shader cameraCopyShader;

        protected override RenderPipeline CreatePipeline()
        {
            return new ArcToonRenderPipelineInstance(
                new ArcToonRenderPipelineParams
                {
                    enableSRPBatcher = enableSRPBatcher,
                    enableGPUInstancing = enableGPUInstancing,
                    colorLUTResolution = (int)colorLUTResolution,
                },
                globalShadowSettings, 
                enablePostProcessing ? globalPostFXSettings : null, 
                cameraBufferSettings,
                cameraCopyShader
            );
        }
    }
}