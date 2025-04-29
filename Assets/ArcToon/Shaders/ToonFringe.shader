Shader "ArcToon/ToonFringe"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        [Toggle(_NORMAL_MAP)] _NormalMapToggle ("Use Normal Map", Float) = 0
        [NoScaleOffset] _NormalMap("Normals", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 1)) = 1

        [Toggle(_RMO_MASK_MAP)] _MaskMapToggle ("Use Mask Map (RMO)", Float) = 0
        [NoScaleOffset] _RMOMaskMap ("Mask (RMO)", 2D) = "white" {}

        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.8
        _Occlusion ("Occlusion", Range(0, 1)) = 1
        _Fresnel ("Fresnel", Range(0, 1)) = 1

        [Toggle(_RAMP_SET)] _RampSetToggle ("Use Ramp Set", Float) = 0
        [NoScaleOffset] _RampSet ("Ramp Set", 2D) = "white" {}

        _DirectLightAttenOffset ("Direct Attenuation Offset", Range(0, 1)) = 0.5
        _DirectLightAttenSmooth ("Direct Attenuation Smooth", Range(0, 5)) = 0.5
        _DirectLightAttenSmoothNew ("Direct Attenuation Smooth New", Range(0, 1)) = 0.5
        
        [Toggle(_SPEC_MAP)] _SpecMapToggle ("Use Specular Map", Float) = 0
        _SpecMap ("Specular Map", 2D) = "white" {}
        _SpecScale ("Specular Scale", Range(0, 1)) = 1
        
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1

        _OutlineColor ("Outline Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _OutlineScale ("Outline Scale", Range(0, 1)) = 0.1
        
        _RimScale ("Screen Space Rim Light Scale", Range(0, 1)) = 0.5
        _RimWidth ("Screen Space Rim Light Width", Range(0, 1)) = 0.5
        _RimDepthBias ("Screen Space Rim Light Depth Bias", Float) = 3

        [NoScaleOffset] _EmissionMap ("Emission", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission", Color) = (0.0, 0.0, 0.0, 0.0)

        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        
        _FringeShadowBiasScaleX ("Fringe Shadow Bias Scale X", Range(0, 1)) = 0.5
        _FringeShadowBiasScaleY ("Fringe Shadow Bias Scale Y", Range(0, 1)) = 0.5

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
            "Queue" = "Geometry+10"
        }
                
        HLSLINCLUDE
        #include "ToonCoreInput.hlsl"
        ENDHLSL

        UsePass "ArcToon/ToonBase/TOON OUTLINE"
        
        UsePass "ArcToon/ToonBase/TOON BASE"

        Pass
        {
            Tags
            {
                "LightMode" = "FringeCaster"
            }
            Blend One Zero
            ZTest On
            ZWrite Off
            Cull Back
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
                ReadMask 3
                WriteMask 3
            }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "FringeCasterPass.hlsl"

            #pragma vertex FringeCasterPassVertex
            #pragma fragment FringeCasterPassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "EyeLashesCaster"
            }
            Blend One Zero
            ZTest On
            ZWrite Off
            Cull Back
            Stencil
            {
                Ref 4
                Comp Always
                Pass Replace
                ReadMask 12
                WriteMask 12
            }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "EyeLashesCasterPass.hlsl"

            #pragma vertex EyeLashesCasterPassVertex
            #pragma fragment EyeLashesCasterPassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0
            Cull Back

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
                "LightMode" = "DepthOnly"
            }
            ZWrite On
            Cull Back
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "DepthOnlyPass.hlsl"

            #pragma vertex DepthOnlyPassVertex
            #pragma fragment DepthOnlyPassFragment
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

    CustomEditor "ArcToon.Editor.GUI.ArcToonShaderGUI"
}