using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [System.Serializable]
    public class RenderPipelineConfig
    {
        #region Lighting Constants
        public const int MaxDirectionalLightCount = 4;
        public const int MaxSpotLightCount = 64;
        public const int MaxPointLightCount = 16;
        public const int MaxPerObjectCasterCount = 16;
        #endregion

        #region Shadow Constants
        public const int MaxTilesPerLight = 6;
        public const int MaxCascades = 4;
        #endregion
        
        public bool useSRPBatcher = true;

        public CameraBufferSettings cameraBufferSettings;

        public ShadowSettings globalShadowSettings;

        public ForwardPlusSettings forwardPlusSettings;

        [FormerlySerializedAs("globalPostFXSettings")] public PostFXConfig globalPostFXConfig;
    }
}