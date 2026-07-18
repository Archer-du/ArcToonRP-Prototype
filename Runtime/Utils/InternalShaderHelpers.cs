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
            public static readonly string ScreenDebug = "Hidden/ArcToon/Screen Debug";

            // Per-processor post-process shaders.
            public static readonly string PostProcessBloom = "Hidden/ArcToon/PostProcess/Bloom";
            public static readonly string PostProcessColorGrading = "Hidden/ArcToon/PostProcess/ColorGrading";
            public static readonly string PostProcessFXAA = "Hidden/ArcToon/PostProcess/FXAA";
            public static readonly string PostProcessSMAA = "Hidden/ArcToon/PostProcess/SMAA";
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

            #region PostProcessing
            // Bloom
            public static readonly int BloomHighResTexture = ShaderPropertyID();
            public static readonly int BloomThreshold = ShaderPropertyID();
            public static readonly int BloomBicubicUpsampling = ShaderPropertyID();
            public static readonly int BloomScale = ShaderPropertyID();
            public static readonly int BloomScatter = ShaderPropertyID();

            // Color Grading
            public static readonly int ColorGradingLUT = ShaderPropertyID();
            public static readonly int ColorGradingLUTParameters = ShaderPropertyID();
            public static readonly int ColorGradingLUTInLogC = ShaderPropertyID();
            public static readonly int ColorAdjustmentData = ShaderPropertyID();
            public static readonly int ColorFilter = ShaderPropertyID();
            public static readonly int WhiteBalance = ShaderPropertyID();
            public static readonly int SplitToningShadows = ShaderPropertyID();
            public static readonly int SplitToningHighlights = ShaderPropertyID();
            public static readonly int ChannelMixerRed = ShaderPropertyID();
            public static readonly int ChannelMixerGreen = ShaderPropertyID();
            public static readonly int ChannelMixerBlue = ShaderPropertyID();
            public static readonly int SMHShadows = ShaderPropertyID();
            public static readonly int SMHMidtones = ShaderPropertyID();
            public static readonly int SMHHighlights = ShaderPropertyID();
            public static readonly int SMHRange = ShaderPropertyID();

            // FXAA
            public static readonly int FXAAParams = ShaderPropertyID();

            // SMAA
            public static readonly int SMAAMetrics = ShaderPropertyID();
            public static readonly int SMAAAreaTexture = ShaderPropertyID();
            public static readonly int SMAASearchTexture = ShaderPropertyID();
            public static readonly int SMAABlendTexture = ShaderPropertyID();
            #endregion

            #region Region ID
            public static readonly int RegionCount = ShaderPropertyID();
            public static readonly int RegionIDChannel = ShaderPropertyID();
            public static readonly int RegionIDMap = ShaderPropertyID();
            #endregion

            #region Debug
            public static readonly int GeometryDebugMode = ShaderPropertyID();
            public static readonly int DebugOpacity = ShaderPropertyID();
            #endregion
        }
        
        public static class TagId
        {
            public static readonly ShaderTagId LightMode = ShaderTagId();

            public static readonly ShaderTagId ShadowCaster = ShaderTagId();
            public static readonly ShaderTagId GeometryOutline = ShaderTagId();
            public static readonly ShaderTagId GeometryDebug = ShaderTagId();
            
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

            // PostProcessing - FXAA
            public static readonly BuiltinGlobalKeyword FXAA_QUALITY_LOW = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword FXAA_QUALITY_MEDIUM = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword FXAA_ALPHA_CONTAINS_LUMA = ShaderGlobalKeyword();

            // PostProcessing - SMAA
            public static readonly BuiltinGlobalKeyword SMAA_PRESET_LOW = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword SMAA_PRESET_MEDIUM = ShaderGlobalKeyword();
            public static readonly BuiltinGlobalKeyword SMAA_PRESET_HIGH = ShaderGlobalKeyword();
        }
    }
}