Shader "Hidden/ArcToon/Post FX Stack"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "PostFXStackPasses.hlsl"
        ENDHLSL

        Pass
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Prefilter Fireflies"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterFirefliesPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Horizontal"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Vertical"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Additive Combine"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomAdditiveCombinePassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Additive Combine Final"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomAdditiveCombineFinalPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Scatter Combine"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterCombinePassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Scatter Combine Final"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterCombineFinalPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Tone Mapping Reinhard"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ToneMappingReinhardPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Tone Mapping Neutral"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ToneMappingNeutralPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Tone Mapping ACES"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ToneMappingACESPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Copy"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment
            ENDHLSL
        }
    }
}