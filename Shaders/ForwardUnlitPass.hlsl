#ifndef ARCTOON_FORWARD_UNLIT_PASS_INCLUDED
#define ARCTOON_FORWARD_UNLIT_PASS_INCLUDED

#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Common.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float4 vertexColor : VAR_COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ForwardUnlitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    output.baseUV = TransformBaseUV(input.baseUV);
    output.vertexColor = input.color;
    return output;
}

float4 ForwardUnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    InputConfig config = GET_INPUT_CONFIG_WITH_REGION(input.positionCS_SS, input.baseUV, input.vertexColor);
    float4 color = GetAlbedo(config);
    
    #if defined(_CLIPPING)
    clip(color.a - GetAlphaClip(config));
    #endif
    
    return float4(color.rgb, GetFinalAlpha(color.a));;
}

#endif
