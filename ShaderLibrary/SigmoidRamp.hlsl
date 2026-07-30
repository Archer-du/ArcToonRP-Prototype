#ifndef ARCTOON_SIGMOID_RAMP_INCLUDED
#define ARCTOON_SIGMOID_RAMP_INCLUDED

// Library tier: CBUFFER-free math for the Sigmoid/Ramp diffuse attenuation model, peer to
// ShaderLibrary/LinearPartition.hlsl. SigmoidSharp / GetHalfLambertFactor come from
// Common.hlsl, which is transitively included before this file (via SurfaceSampling.hlsl).

// Ramp texture V coordinate for the direct-lighting shadow channel.
#define RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL 0.125

// Sigmoid smoothness is authored in 0..1 editor space and remapped to the sharp exponent used by
// SigmoidSharp. Peer of LinearPartition's band-width param.
float RemapSigmoidSmooth(float smoothNew)
{
    return -0.1 / (smoothNew - 1.001);
}

// Sigmoid diffuse attenuation: the primary lit signal (half-lambert for the body, SDF attenuation
// for the face) and the shadow signal are each sigmoid-thresholded, then min'd. The primary's
// center is per-fragment (offset for the body, clipCenter for the face); the shadow's center is
// always the material offset. Peer of LinearPartition's CalculateAttenuation.
float SigmoidAttenuation(float primary, float primaryCenter, float shadow, float offset, float smooth)
{
    return min(
        SigmoidSharp(primary, primaryCenter, smooth),
        SigmoidSharp(shadow, offset, smooth)
    );
}

#endif
