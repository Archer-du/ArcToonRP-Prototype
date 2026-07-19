#ifndef ARCTOON_DEPTH_STENCIL_PASS_INCLUDED
#define ARCTOON_DEPTH_STENCIL_PASS_INCLUDED

#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Common.hlsl"

struct AttributesDS
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsDS
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsDS DefaultDepthStencilPassVertex(AttributesDS input)
{
    VaryingsDS output = (VaryingsDS)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS_SS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half DefaultDepthStencilPassFragment(VaryingsDS input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS_SS);
    #endif
    return input.positionCS_SS.z;
}

#endif