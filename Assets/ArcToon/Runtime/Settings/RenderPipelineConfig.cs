using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    
    public enum TransparencyMode
    {
        Ordered,
        WeightedAverage,
        DepthPeeling,
        DualDepthPeeling,
    }
    
    [System.Serializable]
    public class RenderPipelineConfig
    {
        public bool useSRPBatcher = true;
        
        public TransparencyMode transparencyMode = TransparencyMode.WeightedAverage;

        public CameraBufferSettings cameraBufferSettings;

        public ShadowSettings globalShadowSettings;

        public ForwardPlusSettings forwardPlusSettings;

        [FormerlySerializedAs("globalPostFXSettings")] public PostFXConfig globalPostFXConfig;
    }
}