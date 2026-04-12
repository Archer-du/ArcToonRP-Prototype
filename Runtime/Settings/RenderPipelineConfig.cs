using ArcToon.Passes.PostProcessing;

namespace ArcToon.Settings
{
    [System.Serializable]
    public class RenderPipelineConfig
    {
        public bool useSRPBatcher = true;
        
        public CameraBufferSettings cameraBufferSettings;

        public ShadowSettings globalShadowSettings;

        public ForwardPlusSettings forwardPlusSettings;

        public PostFXConfig globalPostFXConfig;

        // New post-processing config (coexists with old PostFXConfig during migration)
        public PostProcessConfig globalPostProcessConfig;
    }
}