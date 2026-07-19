#ifndef ARCTOON_HAIR_SPEC_INTERFACE_INCLUDED
#define ARCTOON_HAIR_SPEC_INTERFACE_INCLUDED

// Interface tier: anisotropic hair highlight / parallax specular mask getters.
// Reads the per-material CBUFFER; include after the shader's CBUFFER.
// Requires: SurfaceSampling.hlsl (INPUT_PROP), Common.hlsl (SelectChannelRGB, sampler_linear_clamp).

TEXTURE2D(_TangentShiftMap); SAMPLER(sampler_TangentShiftMap);
TEXTURE2D(_SpecularMask);

float GetSpecGloss()
{
    return max(0.001, INPUT_PROP(_SpecGloss) * 200);
}

float GetSpecScale()
{
    return INPUT_PROP(_SpecScale) * 50;
}

float GetTangentShiftOffset()
{
    return INPUT_PROP(_TangentShiftOffset);
}

float SampleTangentShiftNoise(float2 baseUV)
{
    return clamp(-0.8, 0.8, SAMPLE_TEXTURE2D(_TangentShiftMap, sampler_TangentShiftMap, baseUV).r * 2.0 - 1.0);
}

float GetParallaxSensitivity()
{
    return INPUT_PROP(_ParallaxSensitivity) * 0.1;
}

float GetParallaxOffset()
{
    return INPUT_PROP(_ParallaxOffset);
}

float3 SampleParallaxSpecularMask(float2 hairUV)
{
    float4 sample = SAMPLE_TEXTURE2D(_SpecularMask, sampler_linear_clamp, hairUV);
    return SelectChannelRGB(sample, INPUT_PROP(_SpecularMaskChannel));
}

#endif
