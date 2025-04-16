Shader "ArcToon/Unlit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        [HDR] _BaseColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 1

        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0

        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
    }
    SubShader
    {
        HLSLINCLUDE
        #include "UnlitInput.hlsl"
        ENDHLSL
        Pass
        {
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma shader_feature _CLIPPING

            #include "UnlitPass.hlsl"

            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
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
    }

    CustomEditor "ArcToon.Editor.GUI.CustomShaderGUI"
}