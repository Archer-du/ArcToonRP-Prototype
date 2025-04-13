Shader "ArcToon/SimpleLit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        [Toggle(_NORMAL_MAP)] _NormalMapToggle ("Normal Map", Float) = 0
        [NoScaleOffset] _NormalMap("Normals", 2D) = "bump" {}
		_NormalScale("Normal Scale", Range(0, 1)) = 1
        
        [Toggle(_MASK_MAP)] _MaskMapToggle ("Mask Map", Float) = 0
        [NoScaleOffset] _MODSMaskMap("Mask (MODS)", 2D) = "white" {}
        
        [Toggle(_DETAIL_MAP)] _DetailMapToggle ("Detail Maps", Float) = 0
        _DetailMap("Details", 2D) = "linearGrey" {}
        _DetailAlbedo("Detail Albedo", Range(0, 1)) = 1
        _DetailSmoothness("Detail Smoothness", Range(0, 1)) = 1
        [NoScaleOffset] _DetailNormalMap("Detail Normals", 2D) = "bump" {}
        _DetailNormalScale("Detail Normal Scale", Range(0, 1)) = 1
        
        _Metallic ("Metallic", Range(0, 1)) = 0.8
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Fresnel ("Fresnel", Range(0, 1)) = 1
        _Occlusion ("Occlusion", Range(0, 1)) = 1
        
        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
		[HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 1

        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0
        
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
        
        // for hard-coded unity capacity
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    SubShader
    {
        HLSLINCLUDE
		#include "SimpleLitInput.hlsl"
		ENDHLSL

        Pass
        {
            Tags
            {
                "LightMode" = "SimpleLit"
            }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _SPOT_PCF3 _SPOT_PCF5 _SPOT_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma shader_feature _NORMAL_MAP
            #pragma shader_feature _MASK_MAP
            #pragma shader_feature _DETAIL_MAP
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _RECEIVE_SHADOWS

            #include "SimpleLitPass.hlsl"

            #pragma vertex SimplelitPassVertex
            #pragma fragment SimplelitPassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER

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

    CustomEditor "ArcToon.Editor.GUI.CustomShaderGUI"
}