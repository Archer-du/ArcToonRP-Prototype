Shader "ArcToon/ToonBase"
{
    Properties
    {
        // ------------------------ general
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        [NoScaleOffset] _NormalMap ("Normals", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 1)) = 1
        
        _SpecularMask ("Parallax Specular Map", 2D) = "white" {}
        [Enum(UV0, 0, UV1, 1)]
        _SpecularMaskUV ("Parallax Specular Map UV", Integer) = 1
        [Enum(RGB, 0, R, 1, G, 2, B, 3, A, 4)]
        _SpecularMaskChannel ("Specular Mask Channel", Integer) = 0
        
        _ParallaxSensitivity ("Parallax Sensitivity", Range(0, 1)) = 0.1
        _ParallaxOffset ("Parallax Offset", Range(0, 1)) = 0
        
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
        [Toggle(_RECEIVE_FRINGE_SHADOWS)] _ReceiveFringeShadows ("Receive Fringe Shadows", Float) = 0
        [Enum(ArcToon.Editor.ShaderEditor.ShadowCasterOption)] _Shadows ("Shadow Caster Option", Float) = 0
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend Factor", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write Mode", Float) = 1
                
        _StencilEnabled("Stencil Enabled", Float) = 0
        _Stencil("Stencil Ref ID", Float) = 1
        _StencilWriteMask("Stencil Write Mask", Float) = 3
        _StencilReadMask("Stencil Read Mask", Float) = 3
        
        // ------------------------ PBR
        [NoScaleOffset] _MetallicMap ("Metallic Map", 2D) = "white" {}
        [Enum(R, 0, G, 1, B, 2, A, 3)] _MetallicMapChannel ("Metallic Channel", Integer) = 1
        [NoScaleOffset] _RoughnessMap ("Roughness Map", 2D) = "white" {}
        [Enum(R, 0, G, 1, B, 2, A, 3)] _RoughnessMapChannel ("Roughness Channel", Integer) = 0
        [Enum(Roughness, 0, Smoothness, 1)] _RoughnessSource ("Roughness Source", Integer) = 0
        [NoScaleOffset] _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        [Enum(R, 0, G, 1, B, 2, A, 3)] _OcclusionMapChannel ("Occlusion Channel", Integer) = 2

        _Roughness ("Roughness", Range(0, 1)) = 1
        _Metallic ("Metallic", Range(0, 1)) = 0.8
        _Occlusion ("Occlusion", Range(0, 1)) = 1
        _Fresnel ("Fresnel", Range(0, 1)) = 1

        [NoScaleOffset] _EmissionMap ("Emission", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0.0, 0.0, 0.0, 0.0)

        // ------------------------ Toon
        [NoScaleOffset] _RampSet ("Ramp Set", 2D) = "white" {}

        _DirectLightAttenOffset ("Direct Attenuation Offset", Range(0, 1)) = 0.5
        _DirectLightAttenSmoothNew ("Direct Attenuation Smooth New", Range(0, 1)) = 0.5
        
        _DirectLightSpecOffset ("Direct Specular Offset", Range(0, 1)) = 0.5
        _DirectLightSpecSmooth ("Direct Specular Smooth", Range(0, 1)) = 0.5

        // Linear partition diffuse attenuation (alternative to Ramp/Sigmoid)
        _AttenuationModel ("Attenuation Model", Integer) = 0
        _AlbedoSmoothness ("Albedo Smoothness", Range(0, 1)) = 0.5
        _ShadowFadeTint ("Shadow Fade Tint", Color) = (1, 1, 1, 1)
        _ShadowTint ("Shadow Tint", Color) = (1, 1, 1, 1)
        _ShallowFadeTint ("Shallow Fade Tint", Color) = (1, 1, 1, 1)
        _ShallowTint ("Shallow Tint", Color) = (1, 1, 1, 1)
        _SSSTint ("SSS Tint", Color) = (1, 1, 1, 1)
        _FrontTint ("Front Tint", Color) = (1, 1, 1, 1)
        
        _ShadowColor0 ("Shadow Color 0", Color) = (0.5, 0.5, 0.55, 1)
        _ShadowColor1 ("Shadow Color 1", Color) = (0.5, 0.5, 0.55, 1)
        _ShadowColor2 ("Shadow Color 2", Color) = (0.5, 0.5, 0.55, 1)
        _ShadowColor3 ("Shadow Color 3", Color) = (0.5, 0.5, 0.55, 1)
        _ShadowColor4 ("Shadow Color 4", Color) = (0.5, 0.5, 0.55, 1)
        _ShadowColor5 ("Shadow Color 5", Color) = (0.5, 0.5, 0.55, 1)
        _ShadowColor6 ("Shadow Color 6", Color) = (0.5, 0.5, 0.55, 1)
        _ShadowColor7 ("Shadow Color 7", Color) = (0.5, 0.5, 0.55, 1)
        
        _ShallowColor0 ("Shallow Color 0", Color) = (0.9, 0.9, 0.9, 1)
        _ShallowColor1 ("Shallow Color 1", Color) = (0.9, 0.9, 0.9, 1)
        _ShallowColor2 ("Shallow Color 2", Color) = (0.9, 0.9, 0.9, 1)
        _ShallowColor3 ("Shallow Color 3", Color) = (0.9, 0.9, 0.9, 1)
        _ShallowColor4 ("Shallow Color 4", Color) = (0.9, 0.9, 0.9, 1)
        _ShallowColor5 ("Shallow Color 5", Color) = (0.9, 0.9, 0.9, 1)
        _ShallowColor6 ("Shallow Color 6", Color) = (0.9, 0.9, 0.9, 1)
        _ShallowColor7 ("Shallow Color 7", Color) = (0.9, 0.9, 0.9, 1)

        _OutlineColor0 ("Outline Color 0", Color) = (0.1, 0.1, 0.1, 1.0)
        _OutlineColor1 ("Outline Color 1", Color) = (0.1, 0.1, 0.1, 1.0)
        _OutlineColor2 ("Outline Color 2", Color) = (0.1, 0.1, 0.1, 1.0)
        _OutlineColor3 ("Outline Color 3", Color) = (0.1, 0.1, 0.1, 1.0)
        _OutlineColor4 ("Outline Color 4", Color) = (0.1, 0.1, 0.1, 1.0)
        _OutlineColor5 ("Outline Color 5", Color) = (0.1, 0.1, 0.1, 1.0)
        _OutlineColor6 ("Outline Color 6", Color) = (0.1, 0.1, 0.1, 1.0)
        _OutlineColor7 ("Outline Color 7", Color) = (0.1, 0.1, 0.1, 1.0)
        
        _OutlineScale ("Outline Scale", Range(0, 1)) = 0.1
        [Enum(ArcToon.Editor.ShaderEditor.SmoothNormalSource)]
        _SmoothNormalSource ("Smooth Normal Source", Integer) = 1
        [Enum(ArcToon.Editor.ShaderEditor.SmoothNormalDecoder)]
        _SmoothNormalDecoder ("Smooth Normal Decoder", Integer) = 1
        [Enum(ArcToon.Editor.ShaderEditor.WidthControlMode)]
        _WidthControlMode ("Width Control Mode", Integer) = 1
        [Enum(R, 0, G, 1, B, 2, A, 3)]
        _WidthMaskChannel ("Width Mask Channel", Integer) = 3
        
        _RimScale ("Screen Space Rim Light Scale", Range(0, 1)) = 0.5
        _RimWidth ("Screen Space Rim Light Width", Range(0, 1)) = 0.5
        _RimDepthBias ("Screen Space Rim Light Depth Bias", Float) = 3
        
        _HighlightType ("Override Highlight Type", Integer) = 0
        _SpecGloss ("Spec Gloss", Range(0, 1)) = 0.2
        _SpecScale ("Spec Scale", Range(0, 1)) = 0.6
        
        _TangentShiftMap ("Tangent Shift Map", 2D) = "white" {}
        [Enum(UV0, 0, UV1, 1)]
        _TangentShiftMapUV ("Tangent Shift Map UV", Integer) = 1
        _TangentShiftOffset ("Tangent Shift Offset", Range(-1, 1)) = 0

        // ------------------------ Internal
        [HideInInspector] _PerObjectShadowCasterID("Per Object Shadow Caster ID", Float) = -1

        // ------------------------ Region ID
        _RegionCount ("Region Count", Integer) = 1
        [Enum(R, 0, G, 1, B, 2, A, 3)]
        _RegionIDChannel ("Region ID Channel", Integer) = 0
        [NoScaleOffset] _RegionIDMap ("Region ID Map", 2D) = "black" {}

        // for hard-coded unity capacity
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+10"
        }
        
        HLSLINCLUDE
        // --- pre-CBUFFER Library (dependency-free) ---
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Input/SurfaceSampling.hlsl"
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/RegionID.hlsl"
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Light/ToonLighting.hlsl"

        // --- per-material CBUFFER (this shader's own subset) ---
        UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)

            UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
            UNITY_DEFINE_INSTANCED_PROP(float, _Roughness)
            UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
            UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
            UNITY_DEFINE_INSTANCED_PROP(int, _MetallicMapChannel)
            UNITY_DEFINE_INSTANCED_PROP(int, _RoughnessMapChannel)
            UNITY_DEFINE_INSTANCED_PROP(int, _RoughnessSource)
            UNITY_DEFINE_INSTANCED_PROP(int, _OcclusionMapChannel)
            UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)

            UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightAttenOffset)
            UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightAttenSmoothNew)

            UNITY_DEFINE_INSTANCED_PROP(float, _AlbedoSmoothness)
            UNITY_DEFINE_INSTANCED_PROP(float4, _ShadowFadeTint)
            UNITY_DEFINE_INSTANCED_PROP(float4, _ShadowTint)
            UNITY_DEFINE_INSTANCED_PROP(float4, _ShallowFadeTint)
            UNITY_DEFINE_INSTANCED_PROP(float4, _ShallowTint)
            UNITY_DEFINE_INSTANCED_PROP(float4, _SSSTint)
            UNITY_DEFINE_INSTANCED_PROP(float4, _FrontTint)
            REGION_PROP_DECLARE(float4, _ShadowColor)
            REGION_PROP_DECLARE(float4, _ShallowColor)

            REGION_PROP_DECLARE(float4, _OutlineColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _OutlineScale)
            UNITY_DEFINE_INSTANCED_PROP(int, _WidthMaskChannel)

            UNITY_DEFINE_INSTANCED_PROP(float, _RimScale)
            UNITY_DEFINE_INSTANCED_PROP(float, _RimWidth)
            UNITY_DEFINE_INSTANCED_PROP(float, _RimDepthBias)

            UNITY_DEFINE_INSTANCED_PROP(float, _SpecGloss)
            UNITY_DEFINE_INSTANCED_PROP(float, _SpecScale)
            UNITY_DEFINE_INSTANCED_PROP(int, _SpecularMaskUV)
            UNITY_DEFINE_INSTANCED_PROP(int, _SpecularMaskChannel)
            UNITY_DEFINE_INSTANCED_PROP(float, _ParallaxSensitivity)
            UNITY_DEFINE_INSTANCED_PROP(float, _ParallaxOffset)
            UNITY_DEFINE_INSTANCED_PROP(int, _TangentShiftMapUV)
            UNITY_DEFINE_INSTANCED_PROP(float, _TangentShiftOffset)

            UNITY_DEFINE_INSTANCED_PROP(float, _PerObjectShadowCasterID)

            UNITY_DEFINE_INSTANCED_PROP(int, _RegionCount)
            UNITY_DEFINE_INSTANCED_PROP(int, _RegionIDChannel)
        UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

        // --- post-CBUFFER Interface (dependency-bearing) ---
        #include "Packages/com.arctoon.render-pipeline/Shaders/Interface/SurfaceInterface.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/Interface/OutlineInterface.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/Interface/HairSpecInterface.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/Interface/ToonLightingInterface.hlsl"
        ENDHLSL

        Pass
        {
            Name "Toon Outline"
            Tags
            {
                "LightMode" = "GeometryOutline"
            }
            Blend One Zero, One OneMinusSrcAlpha
            ZTest Always
            ZWrite On
            Cull Front

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing
            
            #pragma shader_feature_local _ _SN_SRC_UV1 _SN_SRC_COLOR
            #pragma shader_feature_local _ _SN_DECODE_RGAG _SN_DECODE_OCT
            #pragma shader_feature_local _ _WIDTH_VERTEX_COLOR

            #pragma shader_feature_local _ _REGION_ID_TEXTURE _REGION_ID_VERTEX_COLOR

            #include "Packages/com.arctoon.render-pipeline/Shaders/GeometryOutlinePass.hlsl"

            #pragma vertex GeometryOutlinePassVertex
            #pragma fragment GeometryOutlinePassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Toon Base"
            Tags
            {
                "LightMode" = "ForwardCore"
            }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ _PCF3X3 _PCF5X5 _PCF7X7 _POISSON_DISK _PCSS
            #pragma multi_compile _ _CASCADE_BLEND_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            #pragma shader_feature _NORMAL_MAP
            
            #pragma shader_feature_local _SPEC_MASK
            #pragma shader_feature_local _SPEC_PARALLAX
            
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _RECEIVE_FRINGE_SHADOWS
            
            #pragma shader_feature _METALLIC_MAP
            #pragma shader_feature _ROUGHNESS_MAP
            #pragma shader_feature _OCCLUSION_MAP
            
            #pragma shader_feature _RAMP_SET
            #pragma shader_feature_local _ATTEN_LINEAR_PARTITION

            #pragma shader_feature_local _OVERRIDE_HIGHLIGHT
            #pragma shader_feature_local _TANGENT_SHIFT_MAP

            #pragma shader_feature_local _ _REGION_ID_TEXTURE _REGION_ID_VERTEX_COLOR

            #include "Packages/com.arctoon.render-pipeline/Shaders/ForwardCorePass.hlsl"

            #pragma vertex ForwardCoreVertex
            #pragma fragment ForwardCoreFragment
            ENDHLSL
        }

        Pass
        {
            Name "Toon Depth Only"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            Blend One Zero
            ZTest LEqual
            ZWrite On
            Cull [_Cull]
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "Packages/com.arctoon.render-pipeline/Shaders/DepthStencilPass.hlsl"

            #pragma vertex DefaultDepthStencilPassVertex
            #pragma fragment DefaultDepthStencilPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Toon Depth Stencil"
            Tags
            {
                "LightMode" = "DepthStencil"
            }
            Blend One Zero
            ZTest LEqual
            ZWrite On
            Cull [_Cull]
            Stencil
            {
                Ref [_Stencil]
                Comp Always
                Pass Replace
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "Packages/com.arctoon.render-pipeline/Shaders/DepthStencilPass.hlsl"

            #pragma vertex DefaultDepthStencilPassVertex
            #pragma fragment DefaultDepthStencilPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Toon Shadow Caster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma shader_feature _CLIPPING
            #pragma shader_feature _SHADOWS_DITHER

            #pragma shader_feature_local _ _REGION_ID_TEXTURE _REGION_ID_VERTEX_COLOR

            #include "Packages/com.arctoon.render-pipeline/Shaders/ShadowCasterPass.hlsl"

            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Toon Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5

            #pragma shader_feature_local _ _REGION_ID_TEXTURE _REGION_ID_VERTEX_COLOR

            #include "Packages/com.arctoon.render-pipeline/Shaders/MetaPass.hlsl"

            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Toon Geometry Debug"
            Tags
            {
                "LightMode" = "GeometryDebug"
            }
            Blend One Zero
            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ _PCF3X3 _PCF5X5 _PCF7X7 _POISSON_DISK _PCSS
            #pragma multi_compile _ _CASCADE_BLEND_SOFT

            #pragma shader_feature _CLIPPING
            #pragma shader_feature _RECEIVE_SHADOWS

            #pragma shader_feature_local _ATTEN_LINEAR_PARTITION

            #pragma shader_feature_local _ _REGION_ID_TEXTURE _REGION_ID_VERTEX_COLOR

            #include "Packages/com.arctoon.render-pipeline/Shaders/Debug/GeometryDebugPass.hlsl"

            #pragma vertex ForwardCoreVertex
            #pragma fragment GeometryDebugPassFragment
            ENDHLSL
        }
    }

    CustomEditor "ArcToon.Editor.ShaderEditor.ArcToonBaseShaderGUI"
}