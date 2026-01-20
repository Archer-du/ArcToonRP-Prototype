#ifndef ARCTOON_TOON_TRANSPARENT_PASS_INCLUDED
#define ARCTOON_TOON_TRANSPARENT_PASS_INCLUDED

#include "ToonBasePass.hlsl"

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

Varyings ToonTransparentPassVertex(Attributes input)
{
    return ToonBasePassVertex(input);
}

FragmentOutput ToonTransparentPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace)
{
    FragmentOutput output;
    float4 calculateColor = ToonBasePassFragment(input, isFrontFace);
    output.accumulateColor = float4(calculateColor.rgb * calculateColor.a, calculateColor.a) *
        WeightedBlendedAlphaDepthWeight(calculateColor.a, input.positionCS_SS.z);
    output.revealage = calculateColor.a;
    return output;
}

#endif