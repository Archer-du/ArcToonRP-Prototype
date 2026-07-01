Shader "Hidden/ArcToon/Screen Debug"
{

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "ScreenDebugPass.hlsl"
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