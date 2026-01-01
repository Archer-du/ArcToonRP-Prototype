using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [System.Serializable]
    public class RenderPipelineSettings
    {
        public bool useSRPBatcher = true;
        
        public CameraBufferSettings cameraBufferSettings = new()
        {
            enableHDR = true,
            renderScale = 1f,
            fxaaSettings = new CameraBufferSettings.FXAASettings
            {
                fixedThreshold = 0.0833f,
                relativeThreshold = 0.166f,
                subpixelBlending = 0.75f,
            }
        };

        public ShadowSettings globalShadowSettings;

        [FormerlySerializedAs("globalPostFXSettings")] public PostFXConfig globalPostFXConfig;
        
        public ForwardPlusSettings forwardPlusSettings = new()
        {
            maxLightsPerTile = 30,
        };
    }
}