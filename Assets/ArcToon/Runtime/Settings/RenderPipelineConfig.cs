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
    }
    
    [System.Serializable]
    public class RenderPipelineConfig
    {
        public bool useSRPBatcher = true;
        
        // TODO: determined by material
        public TransparencyMode transparencyMode = TransparencyMode.WeightedAverage;

        public CameraBufferSettings cameraBufferSettings;

        public ShadowSettings globalShadowSettings;

        public ForwardPlusSettings forwardPlusSettings;

        public PostFXConfig globalPostFXConfig;
    }
}