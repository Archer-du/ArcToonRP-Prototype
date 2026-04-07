#ifndef ARCTOON_SHADOWS_INCLUDED
#define ARCTOON_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_PCF3X3)
    #define SHADOW_FILTER_SAMPLES 4
    #define SHADOW_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_PCF5X5)
    #define SHADOW_FILTER_SAMPLES 9
    #define SHADOW_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_PCF7X7)
    #define SHADOW_FILTER_SAMPLES 16
    #define SHADOW_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#elif defined(_POISSON_DISK) || defined(_PCSS)
    #include "PoissonDisk.hlsl"
#endif

#include "Surface.hlsl"
#include "Common.hlsl"
#include "BitPacking.hlsl"

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_SpotShadowAtlas);
TEXTURE2D_SHADOW(_PointShadowAtlas);
TEXTURE2D_SHADOW(_PerObjectShadowAtlas);

CBUFFER_START(_CustomShadows)
    float4 _DirectionalShadowAtlasSize;
    float4 _SpotShadowAtlasSize;
    float4 _PointShadowAtlasSize;
    float4 _PerObjectAtlasSize;

    float4 _ShadowDistanceFade;
    int _CascadeCount;

    #if defined(_POISSON_DISK) || defined(_PCSS)
    float _PoissonFilterRadius;
    #endif
CBUFFER_END

#include "Packages/com.arctoon.render-pipeline/Runtime/Buffers/ShadowCascadeBufferData.cs.hlsl"

StructuredBuffer<ShadowCascadeBufferData> _ShadowCascadeData;

#include "Packages/com.arctoon.render-pipeline/Runtime/Buffers/ShadowTileBufferData.cs.hlsl"

StructuredBuffer<ShadowTileBufferData> _DirectionalShadowData;
StructuredBuffer<ShadowTileBufferData> _SpotShadowData;
StructuredBuffer<ShadowTileBufferData> _PointShadowData;

#include "Packages/com.arctoon.render-pipeline/Runtime/Buffers/PerObjectShadowBufferData.cs.hlsl"

StructuredBuffer<PerObjectShadowBufferData> _PerObjectShadowData;

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
        float distanceSqr = DistanceSquared(surface.positionWS, bufferData.cullingSphere.xyz);
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

// =============================================
// Basic Sampler (unified core functions)
// =============================================

float SampleShadowAtlas(TEXTURE2D_SHADOW_PARAM(shadowAtlas, shadowSampler), float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(shadowAtlas, shadowSampler, positionSTS);
}

float SampleShadowAtlasClamped(TEXTURE2D_SHADOW_PARAM(shadowAtlas, shadowSampler), float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(shadowAtlas, shadowSampler, positionSTS);
}

// =============================================
// Poisson Disk Filter (unified core functions)
// =============================================
#if defined(_POISSON_DISK) || defined(_PCSS)

float FilterShadowPoisson(TEXTURE2D_SHADOW_PARAM(shadowAtlas, shadowSampler), float3 positionSTS,
    float4 atlasSize, float filterRadius)
{
    float texelSize = atlasSize.y;
    float shadow = 0;
    InitPoissonDisk(positionSTS.xy * atlasSize.z);
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * filterRadius * texelSize;
        shadow += SampleShadowAtlas(
            TEXTURE2D_SHADOW_ARGS(shadowAtlas, shadowSampler),
            float3(positionSTS.xy + offset, positionSTS.z)
        );
    }
    return shadow / POISSON_SAMPLE_COUNT;
}

float FilterShadowPoissonClamped(TEXTURE2D_SHADOW_PARAM(shadowAtlas, shadowSampler), float3 positionSTS,
    float3 bounds, float4 atlasSize, float filterRadius)
{
    float texelSize = atlasSize.y;
    float shadow = 0;
    InitPoissonDisk(positionSTS.xy * atlasSize.z);
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * filterRadius * texelSize;
        shadow += SampleShadowAtlasClamped(
            TEXTURE2D_SHADOW_ARGS(shadowAtlas, shadowSampler),
            float3(positionSTS.xy + offset, positionSTS.z), bounds
        );
    }
    return shadow / POISSON_SAMPLE_COUNT;
}

#endif

// =============================================
// PCSS Blocker Search (unified core functions)
// =============================================
#if defined(_PCSS)

// Returns float2(avgBlockerDepth, blockerCount)
float2 BlockerSearch(TEXTURE2D_PARAM(shadowAtlas, depthSampler), float3 positionSTS,
    float4 atlasSize, float searchRadius)
{
    float texelSize = atlasSize.y;
    float blockerDepthSum = 0;
    float blockerCount = 0;
    InitPoissonDisk(positionSTS.xy * atlasSize.z);
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * searchRadius * texelSize;
        float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(
            shadowAtlas, depthSampler,
            positionSTS.xy + offset, 0
        ).r;
        #if defined(UNITY_REVERSED_Z)
        if (shadowMapDepth > positionSTS.z)
        #else
        if (shadowMapDepth < positionSTS.z)
        #endif
        {
            blockerDepthSum += shadowMapDepth;
            blockerCount += 1.0;
        }
    }
    return float2(blockerDepthSum / max(blockerCount, 0.001), blockerCount);
}

// Returns float2(avgBlockerDepth, blockerCount)
float2 BlockerSearchClamped(TEXTURE2D_PARAM(shadowAtlas, depthSampler), float3 positionSTS,
    float3 bounds, float4 atlasSize, float searchRadius)
{
    float texelSize = atlasSize.y;
    float blockerDepthSum = 0;
    float blockerCount = 0;
    InitPoissonDisk(positionSTS.xy * atlasSize.z);
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * searchRadius * texelSize;
        float2 samplePos = clamp(positionSTS.xy + offset, bounds.xy, bounds.xy + bounds.z);
        float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(
            shadowAtlas, depthSampler,
            samplePos, 0
        ).r;
        #if defined(UNITY_REVERSED_Z)
        if (shadowMapDepth > positionSTS.z)
        #else
        if (shadowMapDepth < positionSTS.z)
        #endif
        {
            blockerDepthSum += shadowMapDepth;
            blockerCount += 1.0;
        }
    }
    return float2(blockerDepthSum / max(blockerCount, 0.001), blockerCount);
}

// =============================================
// PCSS Penumbra Estimation (unified helper)
// =============================================

// Computes penumbra width based on projection type.
// Orthographic (directional): depth is linear, penumbra = lightSize * depthDiff
// Perspective (spot/point):   classic PCSS formula = lightSize * depthDiff / blockerDepth
float EstimatePenumbraWidth(float lightSize, float zReceiver, float avgBlockerDepth, bool isOrthographic)
{
    #if defined(UNITY_REVERSED_Z)
    float depthDiff = avgBlockerDepth - zReceiver;
    #else
    float depthDiff = zReceiver - avgBlockerDepth;
    #endif

    float penumbraWidth = isOrthographic
        ? lightSize * depthDiff
        : lightSize * depthDiff / max(avgBlockerDepth, 0.001);

    return max(penumbraWidth, 0.0);
}

// =============================================
// PCSS Main (unified core functions)
// =============================================

float FilterShadowPCSS(
    TEXTURE2D_SHADOW_PARAM(shadowAtlas, cmpSampler),
    TEXTURE2D_PARAM(shadowAtlasLod, depthSampler),
    float3 positionSTS, float4 atlasSize, float lightSize, bool isOrthographic)
{
    float searchRadius = lightSize * _PoissonFilterRadius;

    // Step 1: Blocker Search
    float2 blockerInfo = BlockerSearch(
        TEXTURE2D_ARGS(shadowAtlasLod, depthSampler),
        positionSTS, atlasSize, searchRadius
    );
    float avgBlockerDepth = blockerInfo.x;
    float numBlockers = blockerInfo.y;

    if (numBlockers < 0.5) return 1.0;
    if (numBlockers >= POISSON_SAMPLE_COUNT - 0.5) return 0.0;

    // Step 2: Penumbra Estimation
    float penumbraWidth = EstimatePenumbraWidth(lightSize, positionSTS.z, avgBlockerDepth, isOrthographic);

    // Step 3: PCF Filtering
    float dynamicRadius = penumbraWidth * _PoissonFilterRadius;
    return FilterShadowPoisson(
        TEXTURE2D_SHADOW_ARGS(shadowAtlas, cmpSampler),
        positionSTS, atlasSize, dynamicRadius
    );
}

float FilterShadowPCSSClamped(
    TEXTURE2D_SHADOW_PARAM(shadowAtlas, cmpSampler),
    TEXTURE2D_PARAM(shadowAtlasLod, depthSampler),
    float3 positionSTS, float3 bounds, float4 atlasSize, float lightSize, bool isOrthographic)
{
    float searchRadius = lightSize * _PoissonFilterRadius;

    // Step 1: Blocker Search
    float2 blockerInfo = BlockerSearchClamped(
        TEXTURE2D_ARGS(shadowAtlasLod, depthSampler),
        positionSTS, bounds, atlasSize, searchRadius
    );
    float avgBlockerDepth = blockerInfo.x;
    float numBlockers = blockerInfo.y;

    if (numBlockers < 0.5) return 1.0;
    if (numBlockers >= POISSON_SAMPLE_COUNT - 0.5) return 0.0;

    // Step 2: Penumbra Estimation
    float penumbraWidth = EstimatePenumbraWidth(lightSize, positionSTS.z, avgBlockerDepth, isOrthographic);

    // Step 3: PCF Filtering
    float dynamicRadius = penumbraWidth * _PoissonFilterRadius;
    return FilterShadowPoissonClamped(
        TEXTURE2D_SHADOW_ARGS(shadowAtlas, cmpSampler),
        positionSTS, bounds, atlasSize, dynamicRadius
    );
}

#endif // _PCSS

// =============================================
// Light Type Filter Entry
// =============================================

float FilterDirectionalShadow(float3 positionSTS, float lightSize)
{
    #if defined(_PCSS)
    return FilterShadowPCSS(
        TEXTURE2D_SHADOW_ARGS(_DirectionalShadowAtlas, sampler_linear_clamp_compare),
        TEXTURE2D_ARGS(_DirectionalShadowAtlas, sampler_linear_clamp),
        positionSTS, _DirectionalShadowAtlasSize, lightSize, true
    );
    #elif defined(_POISSON_DISK)
    return FilterShadowPoisson(
        TEXTURE2D_SHADOW_ARGS(_DirectionalShadowAtlas, sampler_linear_clamp_compare),
        positionSTS, _DirectionalShadowAtlasSize, _PoissonFilterRadius
    );
    #elif defined(SHADOW_FILTER_SETUP)
    float weights[SHADOW_FILTER_SAMPLES];
    float2 positions[SHADOW_FILTER_SAMPLES];
    float4 size = _DirectionalShadowAtlasSize;
    SHADOW_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < SHADOW_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleShadowAtlas(
            TEXTURE2D_SHADOW_ARGS(_DirectionalShadowAtlas, sampler_linear_clamp_compare),
            float3(positions[i].xy, positionSTS.z)
        );
    }
    return shadow;
    #else
    return SampleShadowAtlas(
        TEXTURE2D_SHADOW_ARGS(_DirectionalShadowAtlas, sampler_linear_clamp_compare),
        positionSTS
    );
    #endif
}

float FilterPerObjectShadow(float3 positionSTS, float lightSize)
{
    #if defined(_PCSS)
    return FilterShadowPCSS(
        TEXTURE2D_SHADOW_ARGS(_PerObjectShadowAtlas, sampler_linear_clamp_compare),
        TEXTURE2D_ARGS(_PerObjectShadowAtlas, sampler_linear_clamp),
        positionSTS, _PerObjectAtlasSize, lightSize, true
    );
    #elif defined(_POISSON_DISK)
    return FilterShadowPoisson(
        TEXTURE2D_SHADOW_ARGS(_PerObjectShadowAtlas, sampler_linear_clamp_compare),
        positionSTS, _PerObjectAtlasSize, _PoissonFilterRadius
    );
    #elif defined(SHADOW_FILTER_SETUP)
    float weights[SHADOW_FILTER_SAMPLES];
    float2 positions[SHADOW_FILTER_SAMPLES];
    float4 size = _PerObjectAtlasSize;
    SHADOW_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < SHADOW_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleShadowAtlas(
            TEXTURE2D_SHADOW_ARGS(_PerObjectShadowAtlas, sampler_linear_clamp_compare),
            float3(positions[i].xy, positionSTS.z)
        );
    }
    return shadow;
    #else
    return SampleShadowAtlas(
        TEXTURE2D_SHADOW_ARGS(_PerObjectShadowAtlas, sampler_linear_clamp_compare),
        positionSTS
    );
    #endif
}

float FilterSpotShadow(float3 positionSTS, float3 bounds, float lightSize)
{
    #if defined(_PCSS)
    return FilterShadowPCSSClamped(
        TEXTURE2D_SHADOW_ARGS(_SpotShadowAtlas, sampler_linear_clamp_compare),
        TEXTURE2D_ARGS(_SpotShadowAtlas, sampler_linear_clamp),
        positionSTS, bounds, _SpotShadowAtlasSize, lightSize, false
    );
    #elif defined(_POISSON_DISK)
    return FilterShadowPoissonClamped(
        TEXTURE2D_SHADOW_ARGS(_SpotShadowAtlas, sampler_linear_clamp_compare),
        positionSTS, bounds, _SpotShadowAtlasSize, _PoissonFilterRadius
    );
    #elif defined(SHADOW_FILTER_SETUP)
    real weights[SHADOW_FILTER_SAMPLES];
    real2 positions[SHADOW_FILTER_SAMPLES];
    float4 size = _SpotShadowAtlasSize;
    SHADOW_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < SHADOW_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleShadowAtlasClamped(
            TEXTURE2D_SHADOW_ARGS(_SpotShadowAtlas, sampler_linear_clamp_compare),
            float3(positions[i].xy, positionSTS.z), bounds
        );
    }
    return shadow;
    #else
    return SampleShadowAtlasClamped(
        TEXTURE2D_SHADOW_ARGS(_SpotShadowAtlas, sampler_linear_clamp_compare),
        positionSTS, bounds
    );
    #endif
}

float FilterPointShadow(float3 positionSTS, float3 bounds, float lightSize)
{
    #if defined(_PCSS)
    return FilterShadowPCSSClamped(
        TEXTURE2D_SHADOW_ARGS(_PointShadowAtlas, sampler_linear_clamp_compare),
        TEXTURE2D_ARGS(_PointShadowAtlas, sampler_linear_clamp),
        positionSTS, bounds, _PointShadowAtlasSize, lightSize, false
    );
    #elif defined(_POISSON_DISK)
    return FilterShadowPoissonClamped(
        TEXTURE2D_SHADOW_ARGS(_PointShadowAtlas, sampler_linear_clamp_compare),
        positionSTS, bounds, _PointShadowAtlasSize, _PoissonFilterRadius
    );
    #elif defined(SHADOW_FILTER_SETUP)
    real weights[SHADOW_FILTER_SAMPLES];
    real2 positions[SHADOW_FILTER_SAMPLES];
    float4 size = _PointShadowAtlasSize;
    SHADOW_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < SHADOW_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleShadowAtlasClamped(
            TEXTURE2D_SHADOW_ARGS(_PointShadowAtlas, sampler_linear_clamp_compare),
            float3(positions[i].xy, positionSTS.z), bounds
        );
    }
    return shadow;
    #else
    return SampleShadowAtlasClamped(
        TEXTURE2D_SHADOW_ARGS(_PointShadowAtlas, sampler_linear_clamp_compare),
        positionSTS, bounds
    );
    #endif
}

#endif
