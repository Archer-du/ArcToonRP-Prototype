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
#elif defined(_POISSON_DISK) || defined(_PCSS)
    #include "PoissonDisk.hlsl"
#endif

#include "Surface.hlsl"
#include "Common.hlsl"

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_SpotShadowAtlas);
TEXTURE2D_SHADOW(_PointShadowAtlas);
TEXTURE2D_SHADOW(_PerObjectShadowAtlas);

// TODO: define PCSS sampler
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

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
    #if defined(_PCSS)
    float _PcssLightSize;
    #endif
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

struct PerObjectShadowBufferData
{
    float4 normalBias;
    float4x4 shadowMatrix;
};

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
// Basic sampler
// =============================================

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(
        _DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

float SamplePerObjectShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(
        _PerObjectShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

float SampleSpotShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(
        _SpotShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

float SamplePointShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(
        _PointShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

// =============================================
// Poisson Disk Filter
// =============================================
#if defined(_POISSON_DISK) || defined(_PCSS)

float FilterDirectionalShadowPoisson(float3 positionSTS, float filterRadius)
{
    float texelSize = _DirectionalShadowAtlasSize.y;
    float shadow = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * filterRadius * texelSize;
        shadow += SampleDirectionalShadowAtlas(float3(positionSTS.xy + offset, positionSTS.z));
    }
    return shadow / POISSON_SAMPLE_COUNT;
}

float FilterPerObjectShadowPoisson(float3 positionSTS, float filterRadius)
{
    float texelSize = _PerObjectAtlasSize.y;
    float shadow = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * filterRadius * texelSize;
        shadow += SamplePerObjectShadowAtlas(float3(positionSTS.xy + offset, positionSTS.z));
    }
    return shadow / POISSON_SAMPLE_COUNT;
}

float FilterSpotShadowPoisson(float3 positionSTS, float3 bounds, float filterRadius)
{
    float texelSize = _SpotShadowAtlasSize.y;
    float shadow = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * filterRadius * texelSize;
        float2 samplePos = clamp(positionSTS.xy + offset, bounds.xy, bounds.xy + bounds.z);
        shadow += SAMPLE_TEXTURE2D_SHADOW(_SpotShadowAtlas, SHADOW_SAMPLER, float3(samplePos, positionSTS.z));
    }
    return shadow / POISSON_SAMPLE_COUNT;
}

float FilterPointShadowPoisson(float3 positionSTS, float3 bounds, float filterRadius)
{
    float texelSize = _PointShadowAtlasSize.y;
    float shadow = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * filterRadius * texelSize;
        float2 samplePos = clamp(positionSTS.xy + offset, bounds.xy, bounds.xy + bounds.z);
        shadow += SAMPLE_TEXTURE2D_SHADOW(_PointShadowAtlas, SHADOW_SAMPLER, float3(samplePos, positionSTS.z));
    }
    return shadow / POISSON_SAMPLE_COUNT;
}

#endif

// =============================================
// PCSS Blocker Search
// =============================================
#if defined(_PCSS)

// 返回 float2(avgBlockerDepth, blockerCount)
float2 BlockerSearch_Directional(float3 positionSTS, float searchRadius)
{
    float texelSize = _DirectionalShadowAtlasSize.y;
    float blockerDepthSum = 0;
    float blockerCount = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * searchRadius * texelSize;
        float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(
            _DirectionalShadowAtlas, sampler_linear_clamp,
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

float2 BlockerSearch_PerObject(float3 positionSTS, float searchRadius)
{
    float texelSize = _PerObjectAtlasSize.y;
    float blockerDepthSum = 0;
    float blockerCount = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * searchRadius * texelSize;
        float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(
            _PerObjectShadowAtlas, sampler_linear_clamp,
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

float2 BlockerSearch_Spot(float3 positionSTS, float3 bounds, float searchRadius)
{
    float texelSize = _SpotShadowAtlasSize.y;
    float blockerDepthSum = 0;
    float blockerCount = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * searchRadius * texelSize;
        float2 samplePos = clamp(positionSTS.xy + offset, bounds.xy, bounds.xy + bounds.z);
        float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(
            _SpotShadowAtlas, sampler_linear_clamp,
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

float2 BlockerSearch_Point(float3 positionSTS, float3 bounds, float searchRadius)
{
    float texelSize = _PointShadowAtlasSize.y;
    float blockerDepthSum = 0;
    float blockerCount = 0;
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float2 offset = poissonDisk[i] * searchRadius * texelSize;
        float2 samplePos = clamp(positionSTS.xy + offset, bounds.xy, bounds.xy + bounds.z);
        float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(
            _PointShadowAtlas, sampler_linear_clamp,
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
// PCSS Main
// =============================================

float FilterDirectionalShadowPCSS(float3 positionSTS)
{
    float searchRadius = _PcssLightSize * _PoissonFilterRadius;

    // Step 1: Blocker Search
    float2 blockerInfo = BlockerSearch_Directional(positionSTS, searchRadius);
    float avgBlockerDepth = blockerInfo.x;
    float numBlockers = blockerInfo.y;

    // 无遮挡物，完全照亮
    if (numBlockers < 0.5) return 1.0;
    // 全部遮挡
    if (numBlockers >= POISSON_SAMPLE_COUNT - 0.5) return 0.0;

    // Step 2: Penumbra Estimation
    // 方向光使用正交投影，半影宽度公式简化为线性关系
    float zReceiver = positionSTS.z;
    #if defined(UNITY_REVERSED_Z)
    float penumbraWidth = _PcssLightSize * (avgBlockerDepth - zReceiver) / max(avgBlockerDepth, 0.001);
    #else
    float penumbraWidth = _PcssLightSize * (zReceiver - avgBlockerDepth) / max(avgBlockerDepth, 0.001);
    #endif
    penumbraWidth = max(penumbraWidth, 0.0);

    // Step 3: PCF Filtering
    float dynamicRadius = penumbraWidth * _PoissonFilterRadius;
    return FilterDirectionalShadowPoisson(positionSTS, dynamicRadius);
}

float FilterPerObjectShadowPCSS(float3 positionSTS)
{
    float searchRadius = _PcssLightSize * _PoissonFilterRadius;

    float2 blockerInfo = BlockerSearch_PerObject(positionSTS, searchRadius);
    float avgBlockerDepth = blockerInfo.x;
    float numBlockers = blockerInfo.y;

    if (numBlockers < 0.5) return 1.0;
    if (numBlockers >= POISSON_SAMPLE_COUNT - 0.5) return 0.0;

    float zReceiver = positionSTS.z;
    #if defined(UNITY_REVERSED_Z)
    float penumbraWidth = _PcssLightSize * (avgBlockerDepth - zReceiver) / max(avgBlockerDepth, 0.001);
    #else
    float penumbraWidth = _PcssLightSize * (zReceiver - avgBlockerDepth) / max(avgBlockerDepth, 0.001);
    #endif
    penumbraWidth = max(penumbraWidth, 0.0);

    float dynamicRadius = penumbraWidth * _PoissonFilterRadius;
    return FilterPerObjectShadowPoisson(positionSTS, dynamicRadius);
}

float FilterSpotShadowPCSS(float3 positionSTS, float3 bounds)
{
    float searchRadius = _PcssLightSize * _PoissonFilterRadius;

    float2 blockerInfo = BlockerSearch_Spot(positionSTS, bounds, searchRadius);
    float avgBlockerDepth = blockerInfo.x;
    float numBlockers = blockerInfo.y;

    if (numBlockers < 0.5) return 1.0;
    if (numBlockers >= POISSON_SAMPLE_COUNT - 0.5) return 0.0;

    float zReceiver = positionSTS.z;
    #if defined(UNITY_REVERSED_Z)
    float penumbraWidth = _PcssLightSize * (avgBlockerDepth - zReceiver) / max(avgBlockerDepth, 0.001);
    #else
    float penumbraWidth = _PcssLightSize * (zReceiver - avgBlockerDepth) / max(avgBlockerDepth, 0.001);
    #endif
    penumbraWidth = max(penumbraWidth, 0.0);

    float dynamicRadius = penumbraWidth * _PoissonFilterRadius;
    return FilterSpotShadowPoisson(positionSTS, bounds, dynamicRadius);
}

float FilterPointShadowPCSS(float3 positionSTS, float3 bounds)
{
    float searchRadius = _PcssLightSize * _PoissonFilterRadius;

    float2 blockerInfo = BlockerSearch_Point(positionSTS, bounds, searchRadius);
    float avgBlockerDepth = blockerInfo.x;
    float numBlockers = blockerInfo.y;

    if (numBlockers < 0.5) return 1.0;
    if (numBlockers >= POISSON_SAMPLE_COUNT - 0.5) return 0.0;

    float zReceiver = positionSTS.z;
    #if defined(UNITY_REVERSED_Z)
    float penumbraWidth = _PcssLightSize * (avgBlockerDepth - zReceiver) / max(avgBlockerDepth, 0.001);
    #else
    float penumbraWidth = _PcssLightSize * (zReceiver - avgBlockerDepth) / max(avgBlockerDepth, 0.001);
    #endif
    penumbraWidth = max(penumbraWidth, 0.0);

    float dynamicRadius = penumbraWidth * _PoissonFilterRadius;
    return FilterPointShadowPoisson(positionSTS, bounds, dynamicRadius);
}

#endif // _PCSS

// =============================================
// Light Type Filter Entry
// =============================================

float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(_PCSS)
    return FilterDirectionalShadowPCSS(positionSTS);
    #elif defined(_POISSON_DISK)
    return FilterDirectionalShadowPoisson(positionSTS, _PoissonFilterRadius);
    #elif defined(DIRECTIONAL_FILTER_SETUP)
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

float FilterPerObjectShadow(float3 positionSTS)
{
    #if defined(_PCSS)
    return FilterPerObjectShadowPCSS(positionSTS);
    #elif defined(_POISSON_DISK)
    return FilterPerObjectShadowPoisson(positionSTS, _PoissonFilterRadius);
    #elif defined(DIRECTIONAL_FILTER_SETUP)
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _PerObjectAtlasSize;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SamplePerObjectShadowAtlas(
            float3(positions[i].xy, positionSTS.z)
        );
    }
    return shadow;
    #else
    return SamplePerObjectShadowAtlas(positionSTS);
    #endif
}

float FilterSpotShadow(float3 positionSTS, float3 bounds)
{
    #if defined(_PCSS)
    return FilterSpotShadowPCSS(positionSTS, bounds);
    #elif defined(_POISSON_DISK)
    return FilterSpotShadowPoisson(positionSTS, bounds, _PoissonFilterRadius);
    #elif defined(SPOT_FILTER_SETUP)
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

float FilterPointShadow(float3 positionSTS, float3 bounds)
{
    #if defined(_PCSS)
    return FilterPointShadowPCSS(positionSTS, bounds);
    #elif defined(_POISSON_DISK)
    return FilterPointShadowPoisson(positionSTS, bounds, _PoissonFilterRadius);
    #elif defined(POINT_FILTER_SETUP)
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
