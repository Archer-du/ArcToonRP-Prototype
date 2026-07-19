Shader "Hidden/ArcToon/Screen Debug"
{

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Common.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/Debug/ScreenDebugPass.hlsl"
        ENDHLSL

        Pass
        {
            Name "Forward+ Tile Debug"

            Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
				#pragma target 4.5
				
				#pragma vertex DefaultPassVertex
				#pragma fragment ForwardPlusTilesPassFragment
			ENDHLSL
        }
    }
}