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
        }
        
        public static class TagId
        {
            public static readonly ShaderTagId LightMode = ShaderTagId();
            public static readonly ShaderTagId ShadowCaster = ShaderTagId();
        }
    }
}