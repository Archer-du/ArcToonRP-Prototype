#ifndef ARCTOON_TOON_TRANSPARENT_PASS_INCLUDED
#define ARCTOON_TOON_TRANSPARENT_PASS_INCLUDED

#include "ToonBasePass.hlsl"

struct FragmentOutput
{
    float4 accumulateColor : SV_TARGET0;
    float accumulateComplexity : SV_TARGET1;
};

Varyings ToonTransparentPassVertex(Attributes input)
{
    return ToonBasePassVertex(input);
}

FragmentOutput ToonTransparentPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace)
{
    FragmentOutput output;
    float4 calculateColor = ToonBasePassFragment(input, isFrontFace);
    output.accumulateColor.rgb = calculateColor.rgb * calculateColor.a;
    output.accumulateColor.a = calculateColor.a;
    output.accumulateComplexity = 1;
    return output;
}

#endif