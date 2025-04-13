#ifndef ARCTOON_SHADOWS_INCLUDED
#define ARCTOON_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#if defined(_SPOT_PCF3)
    #define SPOT_FILTER_SAMPLES 4
    #define SPOT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_SPOT_PCF5)
    #define SPOT_FILTER_SAMPLES 9
    #define SPOT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_SPOT_PCF7)
    #define SPOT_FILTER_SAMPLES 16
    #define SPOT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_CASCADE_COUNT 4

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SHADOWED_SPOT_LIGHT_COUNT 16
#define MAX_SHADOWED_POINT_LIGHT_COUNT 2

#include "Surface.hlsl"
#include "Common.hlsl"

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_SpotShadowAtlas);

#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _ShadowAtlasSize;
    float4 _ShadowDistanceFade;

    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4x4 _SpotShadowMatrices[MAX_SHADOWED_SPOT_LIGHT_COUNT];

    float4 _SpotShadowTiles[MAX_SHADOWED_SPOT_LIGHT_COUNT];
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
CBUFFER_END

struct ShadowMask
{
    bool alwaysMode;
    bool distanceMode;
    float4 shadows;
};

struct CascadeShadowData
{
    int offset;
    float blend;
    float rangeFade;
};

CascadeShadowData GetCascadeShadowData(Surface surface)
{
    CascadeShadowData cascade;
    int i;
    cascade.rangeFade = 1.0;
    cascade.blend = 1.0;
    for (i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            float fade = FadedStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            if (i == _CascadeCount - 1)
            {
                cascade.rangeFade *= fade;
            }
            else
            {
                cascade.blend *= fade;
            }
            break;
        }
    }
    // stop sampling if directional & end up beyond the last cascade
    if (i == _CascadeCount)
    {
        cascade.rangeFade = 0.0;
    }
    #if defined(_CASCADE_BLEND_DITHER)
    else if (cascade.blend < surface.dither)
    {
        i += 1;
    }
    #endif

    cascade.offset = i;
    return cascade;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    if (strength <= 0) return 1.0;
    float shadow = 1.0;
    if (mask.alwaysMode || mask.distanceMode)
    {
        if (channel >= 0)
        {
            shadow = mask.shadows[channel];
        }
    }
    return lerp(1.0, shadow, strength);
}

float MixBakedAndRealtimeShadow(float bakedShadow, float realtimeShadow, float fade)
{
    return lerp(bakedShadow, realtimeShadow, fade);
}

// TODO: bounds?
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(
        _DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(DIRECTIONAL_FILTER_SETUP)
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleDirectionalShadowAtlas(
            float3(positions[i].xy, positionSTS.z)
        );
    }
    return shadow;
    #else
    return SampleDirectionalShadowAtlas(positionSTS);
    #endif
}

float SamplePointSpotShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(
        _SpotShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

float FilterPointSpotShadow(float3 positionSTS, float3 bounds)
{
    #if defined(SPOT_FILTER_SETUP)
    real weights[SPOT_FILTER_SAMPLES];
    real2 positions[SPOT_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.wwzz;
    SPOT_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < SPOT_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SamplePointSpotShadowAtlas(
            float3(positions[i].xy, positionSTS.z), bounds
        );
    }
    return shadow;
    #else
    return SamplePointSpotShadowAtlas(positionSTS, bounds);
    #endif
}

#endif
