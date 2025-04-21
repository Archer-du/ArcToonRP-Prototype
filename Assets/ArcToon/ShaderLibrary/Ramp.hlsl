#ifndef ARCTOON_RAMP_INCLUDED
#define ARCTOON_RAMP_INCLUDED

#define RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL 0.125

float3 SampleRampSetChannel(TEXTURE2D_PARAM(RampTex, RampSampler), float rampUV, float channel)
{
    return SAMPLE_TEXTURE2D(RampTex, RampSampler, float2(rampUV, channel));
}

#endif