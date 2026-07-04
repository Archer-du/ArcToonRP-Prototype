#ifndef ARCTOON_GEOMETRY_OUTLINE_PASS_INCLUDED
#define ARCTOON_GEOMETRY_OUTLINE_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

#define OUTLINE_WIDTH_MIN_COEF 0.001
#define OUTLINE_WIDTH_MAX_COEF 0.006

struct AttributesGO
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    float4 UV1 : TEXCOORD1;
    float4 vertexColor : COLOR;
    
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
    return normalize(DecodeOctahedral(sample.xy));
    #elif defined(_SN_DECODE_RGAG)
    return normalize(UnpackNormalmapRGorAG(sample, 1.0));
    #else
    return normalize(UnpackNormalmapRGorAG(sample, 1.0));
    #endif
}

VaryingsGO GeometryOutlinePassVertex(AttributesGO input)
{
    VaryingsGO output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
    float4 tangentWS = TransformObjectToWorldTangent(input.tangentOS);
    #if defined(_SN_SRC_UV1)
    float4 smoothNormalSource = input.UV1;
    float3 smoothNormalWS = NormalTangentToWorld(normalize(DecodeSmoothNormal(smoothNormalSource)),
        normalWS, tangentWS, true);
    #elif defined(_SN_SRC_COLOR)
    float4 smoothNormalSource = input.vertexColor;
    float3 smoothNormalWS = NormalTangentToWorld(normalize(DecodeSmoothNormal(smoothNormalSource)),
        normalWS, tangentWS, true);
    #else
    float3 smoothNormalWS = normalWS;
    #endif
    float3 smoothNormalVS = TransformWorldToViewNormal(smoothNormalWS, true);
    float linearDepth = - positionVS.z;
    float outlineScale = GetOutlineScale();
    #if defined(_WIDTH_VERTEX_COLOR)
    outlineScale *= SelectChannel(input.vertexColor, INPUT_PROP(_WidthMaskChannel));
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
    ClipFragmentDepthTest(fragment.depth, fragment.bufferDepth);
    return float4(GetOutlineColor(), 1.0);
}

#endif