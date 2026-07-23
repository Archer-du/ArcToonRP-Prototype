#ifndef ARCTOON_SIGMOID_RAMP_MODEL_INCLUDED
#define ARCTOON_SIGMOID_RAMP_MODEL_INCLUDED

// Diffuse attenuation model: Sigmoid thresholding, optionally colorized by a Ramp Set texture.
// Peer of LinearPartitionModel.hlsl; exactly one is included by ToonLightingAssembly.hlsl
// (selected by _ATTEN_LINEAR_PARTITION). Provides ShadeDiffuseBody / ShadeDiffuseFace for the
// shared dispatcher. Owns the Ramp Set texture resource. Requires: SigmoidRamp.hlsl
// (SigmoidAttenuation, RemapSigmoidSmooth, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL),
// Common.hlsl (GetHalfLambertFactor), SDFFaceInterface.hlsl (FaceSDFSample, when _SDF_LIGHT_MAP).

TEXTURE2D(_RampSet); SAMPLER(sampler_RampSet);

float3 SampleRampSetChannel(float rampUV, float channel)
{
    return SAMPLE_TEXTURE2D(_RampSet, sampler_RampSet, float2(rampUV, channel)).rgb;
}

float3 SigmoidRampColor(float attenuationUV)
{
    attenuationUV = clamp(attenuationUV, 0.001, 0.999);
    #if defined(_RAMP_SET)
    return SampleRampSetChannel(attenuationUV, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL);
    #else
    return attenuationUV;
    #endif
}

float3 ShadeDiffuseBody(Surface surface, Light light, float shadow)
{
    float halfLambert = GetHalfLambertFactor(surface.normalWS, light.directionWS);
    float offset = INPUT_PROP(_DirectLightAttenOffset);
    float smooth = RemapSigmoidSmooth(INPUT_PROP(_DirectLightAttenSmoothNew));
    return SigmoidRampColor(SigmoidAttenuation(halfLambert, offset, shadow, offset, smooth));
}

#if defined(_SDF_LIGHT_MAP)
float3 ShadeDiffuseFace(Surface surface, FaceSDFSample face, float shadow)
{
    float offset = INPUT_PROP(_DirectLightAttenOffset);
    float smooth = RemapSigmoidSmooth(INPUT_PROP(_DirectLightAttenSmoothNew));
    return SigmoidRampColor(SigmoidAttenuation(face.attenuation, face.clipCenter, shadow, offset, smooth));
}
#endif

#endif
