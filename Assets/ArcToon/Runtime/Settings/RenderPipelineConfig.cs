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
        
        public const int MaxTilesPerLight = 6;
        #endregion

        #region Shadow Constants
        public const int MaxCascades = 4;

        public const int MaxShadowedDirectionalLightCount = 4;
        public const int MaxShadowedSpotLightCount = 16;
        public const int MaxShadowedPointLightCount = 2;
        public const int MaxPerObjectShadowCasterCount = 16;
        #endregion

        #region Global Params
        public bool useSRPBatcher = true;

        public CameraBufferSettings cameraBufferSettings;

        public ShadowSettings globalShadowSettings;

        public ForwardPlusSettings forwardPlusSettings;

        [FormerlySerializedAs("globalPostFXSettings")] public PostFXConfig globalPostFXConfig;
        #endregion
    }
}