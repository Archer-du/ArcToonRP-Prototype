using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Utils
{
    public static class InternalShader
    {
        private static ShaderTagId ShaderTagId([CallerMemberName] string name = null)
        {
            return new ShaderTagId(name);
        }

        private static int ShaderPropertyID([CallerMemberName] string name = null)
        {
            return Shader.PropertyToID("_" + name);
        }
        
        public static class Path
        {
            public static readonly string CameraCopy = "Hidden/ArcToon/Camera Copy";
            public static readonly string CameraDebug = "Hidden/ArcToon/Camera Debug";
        }
        
        public static class PropertyID
        {
            public static readonly int CameraBufferSize = ShaderPropertyID();

            #region LightingPass
            public static readonly int DirectionalLightCount = ShaderPropertyID();
            public static readonly int DirectionalLightData = ShaderPropertyID();

            public static readonly int SpotLightCount = ShaderPropertyID();
            public static readonly int SpotLightData = ShaderPropertyID();

            public static readonly int PointLightCount = ShaderPropertyID();
            public static readonly int PointLightData = ShaderPropertyID();

            public static readonly int PerObjectShadowCasterID = ShaderPropertyID();
            public static readonly int PerObjectShadowCasterCount = ShaderPropertyID();
            public static readonly int PerObjectShadowCasterData = ShaderPropertyID();
            
            public static readonly int ForwardPlusTileData = ShaderPropertyID();
            public static readonly int ForwardPlusTileSettings = ShaderPropertyID();
            #endregion

            #region DepthStencilPrePass
            public static readonly int CameraDepthTexture = ShaderPropertyID();
            public static readonly int StencilMaskTexture = ShaderPropertyID();
            #endregion

            #region ShadowMapRenderer
            public static readonly int ShadowDistanceFade = ShaderPropertyID();
            public static readonly int ShadowPancaking = ShaderPropertyID();

            public static readonly int DirectionalShadowMatrices = ShaderPropertyID();
            public static readonly int ShadowCascadeData = ShaderPropertyID();
            public static readonly int DirectionalShadowAtlasSize = ShaderPropertyID();
            public static readonly int DirectionalShadowAtlas = ShaderPropertyID();
            public static readonly int CascadeCount = ShaderPropertyID();

            public static readonly int PerObjectShadowData = ShaderPropertyID();
            public static readonly int PerObjectAtlasSize = ShaderPropertyID();
            public static readonly int PerObjectShadowAtlas = ShaderPropertyID();

            public static readonly int SpotShadowData = ShaderPropertyID();
            public static readonly int SpotShadowAtlasSize = ShaderPropertyID();
            public static readonly int SpotShadowAtlas = ShaderPropertyID();

            public static readonly int PointShadowData = ShaderPropertyID();
            public static readonly int PointShadowAtlasSize = ShaderPropertyID();
            public static readonly int PointShadowAtlas = ShaderPropertyID();
            #endregion
        }
        
        public static class TagId
        {
            public static readonly ShaderTagId LightMode = ShaderTagId();

            public static readonly ShaderTagId ShadowCaster = ShaderTagId();
            
            public static readonly ShaderTagId DepthOnly = ShaderTagId();
            public static readonly ShaderTagId StencilOnly = ShaderTagId();
            public static readonly ShaderTagId DepthStencil = ShaderTagId();
            
            public static readonly ShaderTagId FringeShadowReceiver = ShaderTagId();
            public static readonly ShaderTagId EyeLashesReceiver = ShaderTagId();
        }
    }
}