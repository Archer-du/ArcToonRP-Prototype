Shader "ArcToon/ToonPupil"
{
    Properties
    {
        // ------------------------ general
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        [NoScaleOffset] _NormalMap ("Normals", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 1)) = 1
        
        _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
        [Enum(On, 0, Dither, 1, Off, 2)] _Shadows ("Shadow Caster Option", Float) = 0
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend Factor", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write Mode", Float) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "ToonCoreInput.hlsl"
        ENDHLSL
        Pass
        {
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing

            #pragma shader_feature _CLIPPING

            #include "ToonPupilPass.hlsl"

            #pragma vertex ToonPupilPassVertex
            #pragma fragment ToonPupilPassFragment
            ENDHLSL
        }


        UsePass "ArcToon/ToonBase/TOON SHADOW CASTER"
        
        UsePass "ArcToon/ToonBase/TOON META"
    }

    CustomEditor "ArcToon.Editor.ShaderEditor.ArcToonShaderGUI"
}