#ifndef ARCTOON_EYE_CASTER_PASS_INCLUDED
#define ARCTOON_EYE_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Light/Lighting.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    // smooth normal
    float4 smoothNormal : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings EyeLashesCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
    float4 tangentWS = TransformObjectToWorldTangent(input.tangentOS);
    float3 smoothNormalWS = NormalTangentToWorld(normalize(DecodeNormal(input.smoothNormal)),
        normalWS, tangentWS, true);
    float3 smoothNormalVS = TransformWorldToViewNormal(smoothNormalWS, true);
    // TODO: config
    float3 scaledPositionVS = positionVS + smoothNormalVS * 0.1 * 0.1;
    output.positionCS_SS = TransformWViewToHClip(scaledPositionVS);
    return output;
}

float4 EyeLashesCasterPassFragment(Varyings input) : SV_TARGET
{
    return 0.0;
}

#endif