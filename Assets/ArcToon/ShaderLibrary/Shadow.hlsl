#ifndef ARCTOON_SHADOWS_INCLUDED
#define ARCTOON_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_PCF3X3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define SPOT_FILTER_SAMPLES 4
    #define POINT_FILTER_SAMPLES 4

    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
    #define SPOT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
    #define POINT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_PCF5X5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define SPOT_FILTER_SAMPLES 9
    #define POINT_FILTER_SAMPLES 9

    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
    #define SPOT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
    #define POINT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_PCF7X7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define SPOT_FILTER_SAMPLES 16
    #define POINT_FILTER_SAMPLES 16

    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
    #define SPOT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
    #define POINT_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif


#include "Surface.hlsl"
#include "Common.hlsl"

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_SpotShadowAtlas);
TEXTURE2D_SHADOW(_PointShadowAtlas);

#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    float4 _DirectionalShadowAtlasSize;
    float4 _SpotShadowAtlasSize;
    float4 _PointShadowAtlasSize;

    float4 _ShadowDistanceFade;
    int _CascadeCount;
CBUFFER_END

struct ShadowCascadeBufferData
{
    float4 cullingSphere;
    float4 data;
};

StructuredBuffer<ShadowCascadeBufferData> _ShadowCascadeData;

StructuredBuffer<float4x4> _DirectionalShadowMatrices;

struct SpotShadowBufferData
{
    // x: tile border start x
    // y: tile border start y
    // z: tile border length
    // w: shadow normal bias scale
    float4 tileData;
    float4x4 shadowMatrix;
};

StructuredBuffer<SpotShadowBufferData> _SpotShadowData;

struct PointShadowBufferData
{
    // x: tile border start x
    // y: tile border start y
    // z: tile border length
    // w: shadow normal bias scale
    float4 tileData;
    float4x4 shadowMatrix;
};

StructuredBuffer<PointShadowBufferData> _PointShadowData;

struct ShadowMask
{
    bool alwaysMode;
    bool distanceMode;
    float4 shadows;
};

struct CascadeShadowData
{
    int offset;
    float softBlend;
    float rangeFade;
};

CascadeShadowData GetCascadeShadowData(Surface surface)
{
    CascadeShadowData cascade;
    int i;
    cascade.rangeFade = 1.0;
    cascade.softBlend = 1.0;
    for (i = 0; i < _CascadeCount; i++)
    {
        ShadowCascadeBufferData bufferData = _ShadowCascadeData[i];
        float distanceSqr = DistanceSquared(surface.position, bufferData.cullingSphere.xyz);
        if (distanceSqr < bufferData.cullingSphere.w)
        {
            float fade = FadedStrength(distanceSqr, bufferData.data.x, _ShadowDistanceFade.z);
            if (i == _CascadeCount - 1)
            {
                cascade.rangeFade *= fade;
            }
            else
            {
                cascade.softBlend *= fade;
            }
            break;
        }
    }
    // stop sampling if directional & end up beyond the last cascade
    if (i == _CascadeCount)
    {
        cascade.rangeFade = 0.0;
    }
    #if !defined(_CASCADE_BLEND_SOFT)
    else if (cascade.softBlend < surface.dither)
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
    float4 size = _DirectionalShadowAtlasSize;
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

float SampleSpotShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(
        _SpotShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

float FilterSpotShadow(float3 positionSTS, float3 bounds)
{
    #if defined(SPOT_FILTER_SETUP)
    real weights[SPOT_FILTER_SAMPLES];
    real2 positions[SPOT_FILTER_SAMPLES];
    float4 size = _SpotShadowAtlasSize;
    SPOT_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < SPOT_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleSpotShadowAtlas(
            float3(positions[i].xy, positionSTS.z), bounds
        );
    }
    return shadow;
    #else
    return SampleSpotShadowAtlas(positionSTS, bounds);
    #endif
}

float SamplePointShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(
        _PointShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

float FilterPointShadow(float3 positionSTS, float3 bounds)
{
    #if defined(POINT_FILTER_SETUP)
    real weights[POINT_FILTER_SAMPLES];
    real2 positions[POINT_FILTER_SAMPLES];
    float4 size = _PointShadowAtlasSize;
    POINT_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < POINT_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SamplePointShadowAtlas(
            float3(positions[i].xy, positionSTS.z), bounds
        );
    }
    return shadow;
    #else
    return SamplePointShadowAtlas(positionSTS, bounds);
    #endif
}

#endif
