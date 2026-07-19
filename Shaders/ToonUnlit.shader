Shader "ArcToon/ToonUnlit"
{
    Properties
    {
        // ------------------------ General
        _BaseMap ("Texture", 2D) = "white" {}
        [HDR] _BaseColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)

        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0

        // ------------------------ Shadow
        [Enum(ArcToon.Editor.ShaderEditor.ShadowCasterOption)] _Shadows ("Shadow Caster Option", Float) = 0

        // ------------------------ Engine
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend Factor", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write Mode", Float) = 1

        _StencilEnabled("Stencil Enabled", Float) = 0
        _Stencil("Stencil Ref ID", Float) = 1
        _StencilWriteMask("Stencil Write Mask", Float) = 3
        _StencilReadMask("Stencil Read Mask", Float) = 3

        // ------------------------ Region ID
        _RegionCount ("Region Count", Integer) = 1
        [Enum(R, 0, G, 1, B, 2, A, 3)]
        _RegionIDChannel ("Region ID Channel", Integer) = 0
        [NoScaleOffset] _RegionIDMap ("Region ID Map", 2D) = "black" {}

        // for hard-coded unity capacity
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        HLSLINCLUDE
        // --- pre-CBUFFER Library (dependency-free) ---
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Input/SurfaceSampling.hlsl"
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/RegionID.hlsl"

        // --- per-material CBUFFER (this shader's own subset) ---
        UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
            UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)

            UNITY_DEFINE_INSTANCED_PROP(int, _RegionCount)
            UNITY_DEFINE_INSTANCED_PROP(int, _RegionIDChannel)
        UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

        // --- post-CBUFFER Interface (dependency-bearing) ---
        #include "Packages/com.arctoon.render-pipeline/Shaders/Interface/UnlitInterface.hlsl"
        ENDHLSL

        Pass
        {
            Name "Toon Unlit"
            Tags
            {
                "LightMode" = "ForwardUnlit"
            }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma shader_feature _CLIPPING

            #pragma shader_feature_local _ _REGION_ID_TEXTURE _REGION_ID_VERTEX_COLOR

            #include "Packages/com.arctoon.render-pipeline/Shaders/ForwardUnlitPass.hlsl"

            #pragma vertex ForwardUnlitPassVertex
            #pragma fragment ForwardUnlitPassFragment
            ENDHLSL
        }

        UsePass "ArcToon/ToonBase/TOON SHADOW CASTER"
    }

    CustomEditor "ArcToon.Editor.ShaderEditor.ArcToonUnlitShaderGUI"
}