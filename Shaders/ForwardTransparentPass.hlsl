#ifndef ARCTOON_FORWARD_TRANSPARENT_PASS_INCLUDED
#define ARCTOON_FORWARD_TRANSPARENT_PASS_INCLUDED

#include "ForwardCorePass.hlsl"

TEXTURE2D(_OpaqueDepthBuffer);
TEXTURE2D(_DualDepthBufferRef);

int _PeelingLayerIndex;

struct FragmentOutput
{
    float4 accumulateColor : SV_TARGET0;
    float revealage : SV_TARGET1;
};

float WeightedBlendedAlphaDepthWeight(float alpha, float depth)
{
    float weight = clamp(
        alpha * 10.0 * pow(1.0 - depth, 3.0),
        1e-2,
        3e3
    );
    return weight;
}

Varyings ForwardTransparentPassVertex(Attributes input)
{
    return ForwardCoreVertex(input);
}

FragmentOutput ForwardTransparentPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace)
{
    FragmentOutput output;
    float4 calculateColor = ForwardCoreFragment(input, isFrontFace);
    output.accumulateColor = float4(calculateColor.rgb * calculateColor.a, calculateColor.a) *
        WeightedBlendedAlphaDepthWeight(calculateColor.a, input.positionCS_SS.z);
    output.revealage = calculateColor.a;
    return output;
}

Varyings ForwardTransparentDepthPeelingPassVertex(Attributes input)
{
    return ForwardCoreVertex(input);
}

float4 ForwardTransparentDepthPeelingPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    float2 screenUV = GetScreenUV(input.positionCS_SS);
    float depth = input.positionCS_SS.z;
    float opaqueLayerDepth = SAMPLE_DEPTH_TEXTURE(_OpaqueDepthBuffer, sampler_point_clamp, screenUV);
    // depth test lequal
    ClipFragmentDepthTest(depth, opaqueLayerDepth);
    float refBufferDepth = SAMPLE_DEPTH_TEXTURE(_DualDepthBufferRef, sampler_point_clamp, screenUV);
    // depth test greater
    if (_PeelingLayerIndex != 0)
    {
        #if UNITY_REVERSED_Z
        if (depth >= refBufferDepth)
        {
            discard;
        }
        #else
        if (depth <= refBufferDepth)
        {
            discard;
        }
        #endif
    }
    return ForwardCoreFragment(input, isFrontFace);
}

#endif