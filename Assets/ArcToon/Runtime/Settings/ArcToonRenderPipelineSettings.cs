using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [System.Serializable]
    public class ArcToonRenderPipelineSettings
    {
        public bool useSRPBatcher = true;
        
        [FormerlySerializedAs("cameraBuffer")] public CameraBufferSettings cameraBufferSettings = new()
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

        public ShadowSettings globalShadowSettings;

        public bool enablePostProcessing = true;

        public PostFXSettings globalPostFXSettings;

        public Shader cameraCopyShader;
    }
}