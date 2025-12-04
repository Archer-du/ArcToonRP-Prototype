#ifndef ARCTOON_TOON_PUPIL_PASS_INCLUDED
#define ARCTOON_TOON_PUPIL_PASS_INCLUDED

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
    float4 color = GetColor(config);
    
    #if defined(_CLIPPING)
    clip(color.a - GetAlphaClip(config));
    #endif
    
    return float4(color.rgb, GetFinalAlpha(config, color.a));;
}

#endif
