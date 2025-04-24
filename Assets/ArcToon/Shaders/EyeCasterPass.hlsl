#ifndef ARCTOON_EYE_CASTER_PASS_INCLUDED
#define ARCTOON_EYE_CASTER_PASS_INCLUDED

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

Varyings EyeCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS_SS = TransformObjectToHClip(input.positionOS);
    
    return output;
}

float4 EyeCasterPassFragment(Varyings input) : SV_TARGET
{
    return 0.0;
}

#endif