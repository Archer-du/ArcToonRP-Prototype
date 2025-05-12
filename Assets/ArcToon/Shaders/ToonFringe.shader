Shader "ArcToon/ToonFringe"
{
    Properties
    {
        // ------------------------ general
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        [Toggle(_NORMAL_MAP)] _NormalMapToggle ("Use Normal Map", Float) = 0
        [NoScaleOffset] _NormalMap ("Normals", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 1)) = 1
        
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
        
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0

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
        [Toggle(_RAMP_SET)] _RampSetToggle ("Use Ramp Set", Float) = 0
        [NoScaleOffset] _RampSet ("Ramp Set", 2D) = "white" {}

        _DirectLightAttenOffset ("Direct Attenuation Offset", Range(0, 1)) = 0.5
        _DirectLightAttenSmoothNew ("Direct Attenuation Smooth New", Range(0, 1)) = 0.5
        
        _OutlineColor ("Outline Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _OutlineScale ("Outline Scale", Range(0, 1)) = 0.1
        
        _RimScale ("Screen Space Rim Light Scale", Range(0, 1)) = 0.5
        _RimWidth ("Screen Space Rim Light Width", Range(0, 1)) = 0.5
        _RimDepthBias ("Screen Space Rim Light Depth Bias", Float) = 3
        
        _HairSpecGloss ("Hair Spec Gloss", Range(0, 1)) = 0.5
        _HairSpecScale ("Hair Spec Scale", Range(0, 1)) = 0.5
        
        [Toggle(_TANGENT_SHIFT_MAP)] _TangentShiftMapToggle ("Use Tangent Shift Map", Float) = 0
        _TangentShiftMap ("Tangent Shift Map", 2D) = "white" {}
        _TangentShiftOffset ("Tangent Shift Offset", Range(-1, 1)) = 0

        [Toggle(_TRANSPARENT_FRINGE)] _TransparentFringeToggle ("Use Transparent Fringe", Float) = 0
        _FringeTransparentScale ("Fringe Transparent Scale", Range(0, 1)) = 0.5
        
        _FringeShadowBiasScaleX ("Fringe Shadow Bias Scale X", Range(0, 1)) = 0.5
        _FringeShadowBiasScaleY ("Fringe Shadow Bias Scale Y", Range(0, 1)) = 0.5

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
            "Queue" = "Geometry+100"
        }
                
        HLSLINCLUDE
        #include "ToonCoreInput.hlsl"
        ENDHLSL

        UsePass "ArcToon/ToonBase/TOON OUTLINE"
        
        Pass
        {
            Name "Toon Hair"
            Tags
            {
                "LightMode" = "ToonForward"
            }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ _PCF3X3 _PCF5X5 _PCF7X7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma shader_feature _NORMAL_MAP
            #pragma shader_feature _RMO_MASK_MAP
            #pragma shader_feature _RAMP_SET
            #pragma shader_feature _TANGENT_SHIFT_MAP
            #pragma shader_feature _TRANSPARENT_FRINGE
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _CLIPPING

            #pragma shader_feature _DEBUG_INCOMING_LIGHT
            #pragma shader_feature _DEBUG_DIRECT_BRDF
            #pragma shader_feature _DEBUG_SPECULAR
            #pragma shader_feature _DEBUG_DIFFUSE

            #include "ToonFringePass.hlsl"

            #pragma vertex ToonFringePassVertex
            #pragma fragment ToonFringePassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "StencilMask"
            }
            Blend One Zero
            ZTest Always
            ZWrite Off
            Cull Back
            Stencil
            {
                Ref 4
                Comp Equal
                Pass Keep
                ReadMask 12
                WriteMask 12
            }
            ColorMask B

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "ToonStencilMaskPass.hlsl"

            #pragma vertex EyeLashesReceiverPassVertex
            #pragma fragment EyeLashesReceiverPassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "StencilOnly"
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

            #include "ToonDepthStencilPass.hlsl"

            #pragma vertex FringeStencilPassPassVertex
            #pragma fragment DefaultDepthStencilPassFragment
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