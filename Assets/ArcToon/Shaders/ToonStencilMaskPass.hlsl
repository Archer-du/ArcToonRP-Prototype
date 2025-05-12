#ifndef ARCTOON_TOON_STENCIL_MASK_PASS_INCLUDED
#define ARCTOON_TOON_STENCIL_MASK_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

struct AttributesSM
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsSM
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VaryingsSM EyeLashesReceiverPassVertex(AttributesSM input)
{
    VaryingsSM output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    return output;
}

float4 EyeLashesReceiverPassFragment(VaryingsSM input) : SV_TARGET
{
    return float4(0.0, 0.0, 1.0, 0.0);
}

VaryingsSM FringeReceiverPassVertex(AttributesSM input)
{
    VaryingsSM output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    return output;
}

float4 FringeReceiverPassFragment(VaryingsSM input) : SV_TARGET
{
    return float4(0.0, 1.0, 0.0, 0.0);
}

#endif