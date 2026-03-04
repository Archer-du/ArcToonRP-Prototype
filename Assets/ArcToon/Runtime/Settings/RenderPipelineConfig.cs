using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [System.Serializable]
    public class RenderPipelineConfig
    {
        public bool useSRPBatcher = true;
        
        public CameraBufferSettings cameraBufferSettings;

        public ShadowSettings globalShadowSettings;

        public ForwardPlusSettings forwardPlusSettings;

        public PostFXConfig globalPostFXConfig;
    }
}