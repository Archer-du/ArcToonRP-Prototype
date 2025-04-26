#ifndef ARCTOON_FRINGE_RECEIVER_PASS_INCLUDED
#define ARCTOON_FRINGE_RECEIVER_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Light/Lighting.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings FringeReceiverPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    return output;
}

float4 FringeReceiverPassFragment(Varyings input) : SV_TARGET
{
    return float4(0.0, 1.0, 0.0, 0.0);
}

#endif