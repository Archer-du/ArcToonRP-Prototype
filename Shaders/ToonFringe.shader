Shader "ArcToon/ToonFringe"
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
        [Toggle(_RECEIVE_FRINGE_SHADOWS)] _ReceiveFringeShadows ("Receive Fringe Shadows", Float) = 0
        [Enum(ArcToon.Editor.ShaderEditor.ShadowCasterOption)] _Shadows ("Shadow Caster Option", Float) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend Factor", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write Mode", Float) = 1
        
        _Stencil("Stencil Ref ID", Float) = 1
        _StencilWriteMask("Stencil Write Mask", Float) = 3
        _StencilReadMask("Stencil Read Mask", Float) = 3

        // ------------------------ PBR
        [NoScaleOffset] _MetallicMap ("Metallic Map", 2D) = "white" {}
        [Enum(R, 0, G, 1, B, 2, A, 3)] _MetallicMapChannel ("Metallic Channel", Integer) = 1
        [NoScaleOffset] _RoughnessMap ("Roughness Map", 2D) = "white" {}
        [Enum(R, 0, G, 1, B, 2, A, 3)] _RoughnessMapChannel ("Roughness Channel", Integer) = 0
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
        
        _OutlineColor ("Outline Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _OutlineScale ("Outline Scale", Range(0, 1)) = 0.1
        [Enum(ArcToon.Editor.ShaderEditor.SmoothNormalSource)]
        _SmoothNormalSource ("Smooth Normal Source", Integer) = 1
        [Enum(ArcToon.Editor.ShaderEditor.SmoothNormalDecoder)]
        _SmoothNormalDecoder ("Smooth Normal Decoder", Integer) = 1
        [Enum(ArcToon.Editor.ShaderEditor.WidthControlMode)]
        _WidthControlMode ("Width Control Mode", Integer) = 1
        
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
        
        _FringeTransparentScale ("Fringe Transparent Scale", Range(0, 1)) = 0.5
        _FringeShadowBiasScaleX ("Fringe Shadow Bias Scale X", Range(0, 1)) = 0.5
        _FringeShadowBiasScaleY ("Fringe Shadow Bias Scale Y", Range(0, 1)) = 0.5

        // ------------------------ Internal
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
        #include "ToonLightingImpl.hlsl"
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
            
            #pragma shader_feature_local _OVERRIDE_HIGHLIGHT
            #pragma shader_feature_local _TANGENT_SHIFT_MAP

            #include "ToonFringePass.hlsl"

            #pragma vertex ToonFringePassVertex
            #pragma fragment ToonFringePassFragment
            ENDHLSL
        }

        UsePass "ArcToon/ToonBase/TOON GEOMETRY DEBUG"

        Pass
        {
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

            #include "ToonDepthStencilPass.hlsl"

            #pragma vertex DefaultDepthStencilPassVertex
            #pragma fragment DefaultDepthStencilPassFragment
            ENDHLSL
        }

        UsePass "ArcToon/ToonBase/TOON SHADOW CASTER"
        
        UsePass "ArcToon/ToonBase/TOON META"
        
        Pass
        {
            Tags
            {
                "LightMode" = "EyeLashesReceiver"
            }
            Blend One Zero
            ZTest On
            ZWrite Off
            Cull [_Cull]
            Stencil
            {
                Ref 2
                Comp Equal
                Pass Keep
                ReadMask 2
                WriteMask 2
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
                "LightMode" = "FringeShadowReceiver"
            }
            Blend One Zero
            ZTest On
            ZWrite Off
            Cull [_Cull]
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
                ReadMask 1
                WriteMask 1
            }
            ColorMask G

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing

            #include "ToonStencilMaskPass.hlsl"

            #pragma vertex FringeReceiverPassVertex
            #pragma fragment FringeReceiverPassFragment
            ENDHLSL
        }
    }

    CustomEditor "ArcToon.Editor.ShaderEditor.ArcToonShaderGUI"
}