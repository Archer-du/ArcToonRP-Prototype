Shader "ArcToon/ToonFace"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.8
        _Occlusion ("Occlusion", Range(0, 1)) = 1
        _Fresnel ("Fresnel", Range(0, 1)) = 1

        [Toggle(_RAMP_SET)] _RampSetToggle ("Use Ramp Set", Float) = 0
        [NoScaleOffset] _RampSet ("Ramp Set", 2D) = "white" {}
        
        [Toggle(_SDF_LIGHT_MAP)] _LightMapSDFToggle ("Use SDF Light Map", Float) = 0
        [Toggle(_SDF_LIGHT_MAP_SPEC)] _LightMapSpecularSDFToggle ("Use SDF Light Map Specular", Float) = 0
        _LightMapSDF ("SDF Light Map", 2D) = "white" {}
        
        _ShadowOffsetSDF ("SDF Light Map Attenuation Offset", Range(-1, 1)) = 0
        _NoseSpecularStrengthSDF ("SDF Light Map Nose Specular Strength", Range(0, 1)) = 0.5
        _NoseSpecularSmoothSDF ("SDF Light Map Nose Specular Smooth", Range(0, 1)) = 0.1
        
        _DirectLightAttenOffset ("Direct Attenuation Offset", Range(0, 1)) = 0.5
        _DirectLightAttenSmooth ("Direct Attenuation Smooth", Range(0, 5)) = 0.5

        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0

        _OutlineColor ("Outline Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _OutlineScale ("Outline Scale", Range(0, 1)) = 0.1

        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)

        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        
        [IntRange] _StencilRef ("Fringe StencilRef (Binary)", Range(0, 255)) = 0
        
        [IntRange] _StencilReadMask ("Fringe Stencil Read Mask (Binary)", Range(0, 255)) = 0
        [IntRange] _StencilWriteMask ("Fringe Stencil Write Mask (Binary)", Range(0, 255)) = 0

        [KeywordEnum(None, IncomingLight, DirectBRDF, Specular, Diffuse)]
        _LightingDebugMode ("Lighting Debug Mode", Float) = 0

        // for hard-coded unity capacity
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    SubShader
    {
        HLSLINCLUDE
        #include "ToonCoreInput.hlsl"
        ENDHLSL

        Pass
        {
            Tags
            {
                "LightMode" = "ToonOutline"
            }
            Blend One Zero, One OneMinusSrcAlpha
            ZWrite On
            Cull Front

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #include "GeometryOutlinePass.hlsl"

            #pragma vertex GeometryOutlinePassVertex
            #pragma fragment GeometryOutlinePassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "FringeReceiver"
            }
            Blend One Zero
            ZTest Always
            ZWrite Off
            Cull Back
            Stencil
            {
                Ref[_StencilRef]
                Comp Equal
                Pass Keep
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }
            ColorMask G

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing

            #include "FringeReceiverPass.hlsl"

            #pragma vertex FringeReceiverPassVertex
            #pragma fragment FringeReceiverPassFragment
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ToonBase"
            }
            Blend One Zero, One OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ _PCF3X3 _PCF5X5 _PCF7X7
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma shader_feature _RAMP_SET
            #pragma shader_feature _SDF_LIGHT_MAP
            #pragma shader_feature _SDF_LIGHT_MAP_SPEC
            
            #pragma shader_feature _CLIPPING

            #pragma shader_feature _DEBUG_INCOMING_LIGHT
            #pragma shader_feature _DEBUG_DIRECT_BRDF
            #pragma shader_feature _DEBUG_SPECULAR
            #pragma shader_feature _DEBUG_DIFFUSE

            #include "ToonFacePass.hlsl"

            #pragma vertex ToonFacePassVertex
            #pragma fragment ToonFacePassFragment
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