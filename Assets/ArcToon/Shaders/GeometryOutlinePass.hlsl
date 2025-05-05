#ifndef ARCTOON_GEOMETRY_OUTLINE_PASS_INCLUDED
#define ARCTOON_GEOMETRY_OUTLINE_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

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

Varyings OriginGeometryOutlinePassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
    float3 normalVS = TransformWorldToViewNormal(normalWS, true);
    float outlineScale = GetOutlineScale();
    float3 scaledPositionVS = positionVS + normalVS * outlineScale * 0.02;
    output.positionCS_SS = TransformWViewToHClip(scaledPositionVS);
    return output;
}

Varyings GeometryOutlinePassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    // return OriginGeometryOutlinePassVertex(input);
    float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
    float4 tangentWS = TransformObjectToWorldTangent(input.tangentOS);
    float3 smoothNormalWS = NormalTangentToWorld(normalize(DecodeNormal(input.smoothNormal)),
        normalWS, tangentWS, true);
    float3 smoothNormalVS = TransformWorldToViewNormal(smoothNormalWS, true);
    float linearDepth = -positionVS.z;
    float outlineScale = GetOutlineScale();
    float outlineFactor = outlineScale * GetTexelSizeWorldSpace(linearDepth);
    // TODO: config
    outlineFactor = clamp(outlineFactor, outlineScale * 0.001, outlineScale * 50);
    float3 scaledPositionVS = positionVS + smoothNormalVS * outlineFactor;
    output.positionCS_SS = TransformWViewToHClip(scaledPositionVS);
    return output;
}

float4 GeometryOutlinePassFragment(Varyings input) : SV_TARGET
{
    return float4(GetOutlineColor(), 1.0);
}

#endif