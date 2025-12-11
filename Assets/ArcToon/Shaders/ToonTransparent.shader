Shader "ArcToon/ToonTransparent"
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
        
        _ParallaxSensitivity ("Parallax Sensitivity", Range(0, 1)) = 0.1
        _ParallaxOffset ("Parallax Offset", Range(0, 1)) = 0
        
        _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
        [Enum(On, 0, Dither, 1, Off, 2)] _Shadows ("Shadow Caster Option", Float) = 0
        
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend Factor", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0

        // ------------------------ PBR
        [Toggle(_RMO_MASK_MAP)] _MaskMapToggle ("Use Mask Map (RMO)", Float) = 0
        [NoScaleOffset] _RMOMaskMap ("Mask (RMO)", 2D) = "white" {}

        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.8
        _Occlusion ("Occlusion", Range(0, 1)) = 1
        _Fresnel ("Fresnel", Range(0, 1)) = 1

        [NoScaleOffset] _EmissionMap ("Emission", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0.0, 0.0, 0.0, 0.0)

        // ------------------------ Toon
        [NoScaleOffset] _RampSet ("Ramp Set", 2D) = "white" {}

        _DirectLightAttenOffset ("Direct Attenuation Offset", Range(0, 1)) = 0.5
        _DirectLightAttenSmooth ("Direct Attenuation Smooth", Range(0, 1)) = 0.5
        _DirectLightAttenSmoothNew ("Direct Attenuation Smooth New", Range(0, 1)) = 0.5
        
        _DirectLightSpecOffset ("Direct Specular Offset", Range(0, 1)) = 0.5
        _DirectLightSpecSmooth ("Direct Specular Smooth", Range(0, 1)) = 0.5

        _OutlineColor ("Outline Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _OutlineScale ("Outline Scale", Range(0, 1)) = 0.1
        [Enum(UV1, 0, VertexColor, 1)]
        _SmoothNormalSource ("Smooth Normal Source", Integer) = 1
        [Enum(RGAG, 0, OCT, 1)]
        _SmoothNormalDecoder ("Smooth Normal Decoder", Integer) = 1
        [Enum(None, 0, VertexColorAlpha, 1)]
        _WidthControlMode ("Width Control Mode", Integer) = 1
        
        _RimScale ("Screen Space Rim Light Scale", Range(0, 1)) = 0.5
        _RimWidth ("Screen Space Rim Light Width", Range(0, 1)) = 0.5
        _RimDepthBias ("Screen Space Rim Light Depth Bias", Float) = 3

        // ------------------------ Debug
        [KeywordEnum(None, IncomingLight, DirectBRDF, Specular, Diffuse)]
        _LightingDebugMode ("Lighting Debug Mode", Float) = 0

        [HideInInspector] _PerObjectShadowCasterID("Per Object Shadow Caster ID", Float) = -1
        
        // for hard-coded unity capacity
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
        }
        
        HLSLINCLUDE
        #include "ToonCoreInput.hlsl"
        #include "ToonLightingImpl.hlsl"
        ENDHLSL

        UsePass "ArcToon/ToonBase/TOON OUTLINE"

        Pass
        {
            Name "Toon Transparent Back Face"
            Tags
            {
                "LightMode" = "ToonForwardTransparentBackFace"
            }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ _PCF3X3 _PCF5X5 _PCF7X7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _NORMAL_MAP
            
            #pragma shader_feature _RMO_MASK_MAP

            #pragma shader_feature _RAMP_SET

            #pragma shader_feature _DEBUG_INCOMING_LIGHT
            #pragma shader_feature _DEBUG_DIRECT_BRDF
            #pragma shader_feature _DEBUG_SPECULAR
            #pragma shader_feature _DEBUG_DIFFUSE

            #include "ToonBasePass.hlsl"

            #pragma vertex ToonBasePassVertex
            #pragma fragment ToonBasePassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Toon Transparent Front Face"
            Tags
            {
                "LightMode" = "ToonForwardTransparentFrontFace"
            }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ _PCF3X3 _PCF5X5 _PCF7X7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _NORMAL_MAP
            
            #pragma shader_feature _RMO_MASK_MAP

            #pragma shader_feature _RAMP_SET

            #pragma shader_feature _DEBUG_INCOMING_LIGHT
            #pragma shader_feature _DEBUG_DIRECT_BRDF
            #pragma shader_feature _DEBUG_SPECULAR
            #pragma shader_feature _DEBUG_DIFFUSE

            #include "ToonBasePass.hlsl"

            #pragma vertex ToonBasePassVertex
            #pragma fragment ToonBasePassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            ZWrite On
            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "ToonDepthStencilPass.hlsl"

            #pragma vertex DefaultDepthStencilPassVertex
            #pragma fragment DefaultDepthStencilPassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma shader_feature _CLIPPING
            #pragma shader_feature _SHADOWS_DITHER

            #include "ShadowCasterPass.hlsl"

            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5

            #include "MetaPass.hlsl"

            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            ENDHLSL
        }
    }

    CustomEditor "ArcToon.Editor.ShaderEditor.ArcToonShaderGUI"
}