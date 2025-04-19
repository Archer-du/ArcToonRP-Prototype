Shader "Hidden/ArcToon/Camera Copy"
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
            #pragma fragment CopyPassFragment
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

            ColorMask 0
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex DefaultPassVertex
            #pragma fragment CopyDepthPassFragment
            ENDHLSL
        }
    }
}