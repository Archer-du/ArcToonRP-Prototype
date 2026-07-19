Shader "Hidden/ArcToon/Blitter"
{

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Common.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/Blit/CameraCopyPass.hlsl"
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

            #include "Packages/com.arctoon.render-pipeline/Shaders/Blit/CompositePass.hlsl"
            
            #pragma vertex DefaultPassVertex
            #pragma fragment WeightedAverageCompositePassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Composite Depth Peeling"
            
            HLSLPROGRAM
            #pragma target 3.5

            #include "Packages/com.arctoon.render-pipeline/Shaders/Blit/CompositePass.hlsl"
            
            #pragma vertex DefaultPassVertex
            #pragma fragment DepthPeelingCompositePassFragment
            ENDHLSL
        }
    }
}