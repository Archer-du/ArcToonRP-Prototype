using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using BuiltinGlobalKeyword = UnityEngine.Rendering.GlobalKeyword;

namespace ArcToon.Utils
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

        private static BuiltinGlobalKeyword ShaderGlobalKeyword([CallerMemberName] string name = null)
        {
            return BuiltinGlobalKeyword.Create("_" + name);
        }
        
        public static class Path
        {
            public static readonly string InternalError = "Hidden/InternalErrorShader";
            public static readonly string Blitter = "Hidden/ArcToon/Blitter";
            public static readonly string CameraDebug = "Hidden/ArcToon/Camera Debug";

            // Legacy uber post-process shader (kept for backward compatibility with Legacy passes).
            public static readonly string PostFXStack = "Hidden/ArcToon/Post FX Stack";

            // Per-processor post-process shaders.
            public static readonly string PostProcessBloom = "Hidden/ArcToon/PostProcess/Bloom";
            public static readonly string PostProcessColorGrading = "Hidden/ArcToon/PostProcess/ColorGrading";
            public static readonly string PostProcessFXAA = "Hidden/ArcToon/PostProcess/FXAA";
        }
        
        public static class PropertyID
        {
            public static readonly int CameraBufferSize = ShaderPropertyID();

            #region Lighting
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

            #region ShadowMap
            public static readonly int ShadowDistanceFade = ShaderPropertyID();
            public static readonly int ShadowPancaking = ShaderPropertyID();

            public static readonly int DirectionalShadowData = ShaderPropertyID();
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

            public static readonly int PoissonFilterRadius = ShaderPropertyID();
            #endregion

            #region Prepass
            public static readonly int CameraDepthTexture = ShaderPropertyID();
            public static readonly int StencilMaskTexture = ShaderPropertyID();
            #endregion

            #region Transparency
            public static readonly int AccumulateRGBA = ShaderPropertyID();
            public static readonly int AccumulateComplexity = ShaderPropertyID();
            public static readonly int BackGroundColor = ShaderPropertyID();
            
            public static readonly int OpaqueColorBuffer = ShaderPropertyID();
            public static readonly int OpaqueDepthBuffer = ShaderPropertyID();
            public static readonly int DualDepthBufferRef = ShaderPropertyID();
            public static readonly int DepthPeelingClips = ShaderPropertyID();
            public static readonly int PeelingLayerIndex = ShaderPropertyID();
            #endregion
            
            #region Blit
            public static readonly int SourceTexture = ShaderPropertyID();
            #endregion

            #region Final
            public static readonly int FinalSrcBlend = ShaderPropertyID();
            public static readonly int FinalDstBlend = ShaderPropertyID();
            public static readonly int CopyBicubic = ShaderPropertyID();
            #endregion
        }
        
        public static class TagId
        {
            public static readonly ShaderTagId LightMode = ShaderTagId();

            public static readonly ShaderTagId ShadowCaster = ShaderTagId();
            public static readonly ShaderTagId GeometryOutline = ShaderTagId();
            
            public static readonly ShaderTagId ToonForward = ShaderTagId();
            public static readonly ShaderTagId ToonForwardDepthPeeling = ShaderTagId();
            public static readonly ShaderTagId ToonForwardWeightedAverage = ShaderTagId();
            public static readonly ShaderTagId ToonForwardTransparentBackFace = ShaderTagId();
            public static readonly ShaderTagId ToonForwardTransparentFrontFace = ShaderTagId();
            
            public static readonly ShaderTagId DepthOnly = ShaderTagId();
            public static readonly ShaderTagId StencilOnly = ShaderTagId();
            public static readonly ShaderTagId DepthStencil = ShaderTagId();
            
            public static readonly ShaderTagId FringeShadowReceiver = ShaderTagId();
            public static readonly ShaderTagId EyeLashesReceiver = ShaderTagId();
        }

        public static class GlobalKeyword
        {
            public static readonly BuiltinGlobalKeyword SHADOW_MASK_ALWAYS = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword SHADOW_MASK_DISTANCE = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword CASCADE_BLEND_SOFT = ShaderGlobalKeyword();
            
            public static readonly BuiltinGlobalKeyword PCF3X3 = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword PCF5X5 = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword PCF7X7 = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword POISSON_DISK = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword PCSS = ShaderGlobalKeyword();
        }
    }
}