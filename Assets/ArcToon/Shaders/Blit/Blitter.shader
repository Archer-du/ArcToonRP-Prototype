Shader "Hidden/ArcToon/Blitter"
{

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "CameraCopyPass.hlsl"
        ENDHLSL

        Pass
        {
            Name "Copy Final"

            Blend [_FinalSrcBlend] [_FinalDstBlend]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex DefaultPassVertex
            #pragma fragment CopyFinalPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Copy Depth"

            ColorMask 0
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex DefaultPassVertex
            #pragma fragment CopyDepthPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Copy Color"

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex DefaultPassVertex
            #pragma fragment CopyColorPassFragment
            ENDHLSL
        }
        
        Pass
        {
            Name "Composite Weighted Average"
            
            HLSLPROGRAM
            #pragma target 3.5

            #include "WeightedAverageCompositePass.hlsl"
            
            #pragma vertex DefaultPassVertex
            #pragma fragment WeightedAverageCompositePassFragment
            ENDHLSL
        }
    }
}