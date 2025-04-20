Shader "Hidden/ArcToon/Camera Debug"
{

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "CameraDebugPass.hlsl"
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