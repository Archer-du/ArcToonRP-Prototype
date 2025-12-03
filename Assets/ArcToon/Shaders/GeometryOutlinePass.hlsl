#ifndef ARCTOON_GEOMETRY_OUTLINE_PASS_INCLUDED
#define ARCTOON_GEOMETRY_OUTLINE_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

#define LEGACY_OUTLINE_WIDTH_COEF 0.02

#define OUTLINE_WIDTH_MIN_COEF 0.001
#define OUTLINE_WIDTH_MAX_COEF 0.006

struct AttributesGO
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    // smooth normal
    #if defined(_SN_SRC_COLOR)
    float4 smoothNormal : COLOR;
    #elif defined(_SN_SRC_UV1)
    float4 smoothNormal : TEXCOORD1;
    #else
    float4 smoothNormal : COLOR;
    #endif
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsGO
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float GetOutlineWidthResolutionAdapter()
{
    return _CameraBufferSize.z / 1440;
}

float3 DecodeSmoothNormal(float4 sample)
{
    #if defined(_SN_DECODE_OCT)
    return normalize(OctahedralDecode(sample.xy));
    #elif defined(_SN_DECODE_RGAG)
    return normalize(UnpackNormalmapRGorAG(sample, 1.0));
    #else
    return normalize(UnpackNormalmapRGorAG(sample, 1.0));
    #endif
}

VaryingsGO LegacyGeometryOutlinePassVertex(AttributesGO input)
{
    VaryingsGO output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
    float3 normalVS = TransformWorldToViewNormal(normalWS, true);
    float outlineScale = GetOutlineScale();
    float3 scaledPositionVS = positionVS + normalVS * outlineScale * LEGACY_OUTLINE_WIDTH_COEF;
    output.positionCS_SS = TransformWViewToHClip(scaledPositionVS);
    return output;
}

VaryingsGO GeometryOutlinePassVertex(AttributesGO input)
{
    VaryingsGO output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
    float4 tangentWS = TransformObjectToWorldTangent(input.tangentOS);
    float3 smoothNormalWS = NormalTangentToWorld(normalize(DecodeSmoothNormal(input.smoothNormal)),
        normalWS, tangentWS, true);
    float3 smoothNormalVS = TransformWorldToViewNormal(smoothNormalWS, true);
    float linearDepth = - positionVS.z;
    float outlineScale = GetOutlineScale();
    #if defined(_WIDTH_VERTCOLORA)
    outlineScale *= input.smoothNormal.a;
    #endif
    float outlineFactor = outlineScale * GetTexelSizeWorldSpace(linearDepth) * GetOutlineWidthResolutionAdapter();
    outlineFactor = clamp(outlineFactor,
        outlineScale * OUTLINE_WIDTH_MIN_COEF,
        outlineScale * OUTLINE_WIDTH_MAX_COEF);
    float3 scaledPositionVS = positionVS + smoothNormalVS * outlineFactor;
    output.positionCS_SS = TransformWViewToHClip(scaledPositionVS);
    return output;
}

float4 GeometryOutlinePassFragment(VaryingsGO input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    Fragment fragment = GetFragment(input.positionCS_SS);
    clip(fragment.depth - fragment.bufferDepth);
    return float4(GetOutlineColor(), 1.0);
}

#endif