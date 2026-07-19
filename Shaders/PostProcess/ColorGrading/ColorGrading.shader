Shader "Hidden/ArcToon/PostProcess/ColorGrading"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Common.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/PostProcess/ColorGrading/ColorGradingPasses.hlsl"
        ENDHLSL

        Pass
        {
            Name "Color Grading Only"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingOnlyPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading Reinhard"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingReinhardPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading Neutral"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingNeutralPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading ACES"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingACESPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading LUT Apply"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingFinalPassFragment
            ENDHLSL
        }
    }
}
