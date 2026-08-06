Shader "Hidden/ArcToon/PostProcess/TemporalAA"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Common.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/PostProcess/AA/TemporalAAPass.hlsl"
        ENDHLSL

        Pass
        {
            Name "TAA Copy"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment TemporalAACopyPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "TAA Resolve"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment TemporalAAResolvePassFragment
            ENDHLSL
        }
    }
}
