#ifndef ARCTOON_RAMP_INCLUDED
#define ARCTOON_RAMP_INCLUDED

#define RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL 0.125
#define RAMP_DIRECT_LIGHTING_SPECULAR_CHANNEL 0.375

struct RampChannelData
{
    float directLightAttenSigmoidCenter;
    float directLightAttenSigmoidSharp;
    float directLightSpecSigmoidCenter;
    float directLightSpecSigmoidSharp;
};

RampChannelData GetRampChannelData(
    float directLightAttenSigmoidCenter,
    float directLightAttenSigmoidSharp,
    float directLightSpecSigmoidCenter,
    float directLightSpecSigmoidSharp)
{
    RampChannelData data;
    data.directLightAttenSigmoidCenter = directLightAttenSigmoidCenter;
    data.directLightAttenSigmoidSharp = directLightAttenSigmoidSharp;
    data.directLightSpecSigmoidCenter = directLightSpecSigmoidCenter;
    data.directLightSpecSigmoidSharp = directLightSpecSigmoidSharp;
    return data;
}

float3 SampleRampSetChannel(TEXTURE2D_PARAM(RampTex, RampSampler), float rampUV, float channel)
{
    return SAMPLE_TEXTURE2D(RampTex, RampSampler, float2(rampUV, channel));
}

#endif
