#ifndef ARCTOON_TOON_PUPIL_PASS_INCLUDED
#define ARCTOON_TOON_PUPIL_PASS_INCLUDED

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    float2 UV1 : TEXCOORD1;
    float4 vertexColor : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_ATTRIBUTES_DATA
};

struct VaryingsPupil
{
    float4 positionCS_SS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VaryingsPupil ToonPupilPassVertex(Attributes input)
{
    VaryingsPupil output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 ToonPupilPassFragment(VaryingsPupil input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
    float4 color = GetAlbedo(config);
    
    #if defined(_CLIPPING)
    clip(color.a - GetAlphaClip(config));
    #endif
    
    return float4(color.rgb, color.a);
}

#endif
