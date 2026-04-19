using System;
using ArcToon.Config;
using ArcToon.Passes.PostProcessing;
using UnityEngine.Serialization;

namespace ArcToon.Settings
{
    [Serializable]
    public class RenderPipelineConfig
    {
        public bool useSRPBatcher = true;
        
        public CameraBufferSettings cameraBufferSettings;

        [FormerlySerializedAs("globalShadowSettings")] 
        public ShadowSettings shadowSettings;

        public ForwardPlusSettings forwardPlusSettings;

        public PostProcessConfig globalPostProcessConfig;
    }
}