Shader "Hidden/ArcToon/PostProcess/FXAA"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        Pass
        {
            Name "FXAA"

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile _ _FXAA_QUALITY_MEDIUM _FXAA_QUALITY_LOW
            #pragma multi_compile _ _FXAA_ALPHA_CONTAINS_LUMA

            #include "../../../ShaderLibrary/Common.hlsl"
            #include "FXAAPass.hlsl"

            #pragma vertex DefaultPassVertex
            #pragma fragment FXAAPassFragment
            ENDHLSL
        }
    }
}
