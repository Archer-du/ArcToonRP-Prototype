using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Settings
{
    public static class RenderPipelineInfo
    {
        public static readonly bool CopyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
        
        public static readonly Rect FullViewRect = new(0f, 0f, 1f, 1f);
        
        public const int MaxDirectionalLightCount = 4;
        public const int MaxSpotLightCount = 64;
        public const int MaxPointLightCount = 16;
        public const int MaxPerObjectCasterCount = 16;
        // TODO: should never be modified
        public const int MaxTilesPerLight = 6;
        
        public const int MaxCascades = 4;
        public const int MaxShadowedDirectionalLightCount = 4;
        public const int MaxShadowedSpotLightCount = 16;
        public const int MaxShadowedPointLightCount = 2;
        public const int MaxPerObjectShadowCasterCount = 16;
    }
}