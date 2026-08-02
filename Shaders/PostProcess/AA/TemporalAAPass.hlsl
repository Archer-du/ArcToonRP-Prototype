#ifndef ARCTOON_TEMPORAL_AA_PASS_INCLUDED
#define ARCTOON_TEMPORAL_AA_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.arctoon.render-pipeline/Shaders/PostProcess/PostProcessInput.hlsl"

// _CameraDepthTexture and sampler_point_clamp / sampler_linear_clamp are already declared by the
// Common.hlsl -> Fragment.hlsl include chain; do not redeclare them here.

// --- resolve inputs (set from TemporalAAProcessor) ---
// Jittered current inverse view-projection: reconstructs the true world position from the (jittered)
// depth buffer. Only clip-space XY carries the jitter, so the sampled depth itself is jitter-free.
float4x4 _TAAInvViewProjCurr;
// Non-jittered previous view-projection: reprojects that world position into last frame's resolved
// history, which is effectively jitter-free once accumulated.
float4x4 _TAAViewProjPrev;
// Current-frame blend weight (alpha). History weight is (1 - alpha).
float _TAAFrameInfluence;
// Variance clipping tightness (gamma): the color box is mean +- gamma * stdDev. Larger keeps more
// history (steadier, more ghosting); smaller rejects more (crisper, more flicker). Reference ~1.0.
float _TAAVarianceClampScale;

TEXTURE2D(_TAAHistoryTexture);
float4 _TAAHistoryTexture_TexelSize;

// Straight pass-through, used to prime the history buffer on the first frame.
float4 TemporalAACopyPassFragment(Varyings_Default input) : SV_TARGET
{
    return SampleSource(input.screenUV);
}

float3 SampleSourceRGB(float2 uv, float2 texelOffset)
{
    return SampleSource(uv + texelOffset * GetSourceTexelSize().xy).rgb;
}

// Camera/static reprojection (Survey Eq.1: p_{n-1} = M_{n-1} . M_n^{-1} . p_n).
// Reconstruct the world position from depth using the current (jittered) inverse VP, then project
// it through the previous (non-jittered) VP to find where it sat in the history buffer.
float2 ReprojectToHistoryUV(float2 uv)
{
    float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, uv);
#if !UNITY_REVERSED_Z
    // ComputeWorldSpacePosition expects the depth in the platform's NDC-z range.
    deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, deviceDepth);
#endif

    float3 positionWS = ComputeWorldSpacePosition(uv, deviceDepth, _TAAInvViewProjCurr);
    float4 previousCS = mul(_TAAViewProjPrev, float4(positionWS, 1.0));
    float2 previousNDC = previousCS.xy / previousCS.w;

    // NDC (-1..1) -> UV (0..1), reversing the clip-space Y flip baked into the GPU matrices.
    float2 historyUV = previousNDC * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    historyUV.y = 1.0 - historyUV.y;
#endif
    return historyUV;
}

// Catmull-Rom history resampling (TAA_Guide 3.5; optimized 5-tap after Filmic SMAA / HDRP).
// Bilinear reprojection accumulates blur frame to frame; the sharper cubic kernel preserves detail.
float3 SampleHistoryTap(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_TAAHistoryTexture, sampler_linear_clamp, uv, 0).rgb;
}

float3 SampleHistoryBicubic(float2 uv)
{
    float2 texelSize = _TAAHistoryTexture_TexelSize.xy;
    float2 textureSize = _TAAHistoryTexture_TexelSize.zw;

    float2 samplePos = uv * textureSize;
    float2 tc1 = floor(samplePos - 0.5) + 0.5;
    float2 f = samplePos - tc1;
    float2 f2 = f * f;
    float2 f3 = f * f2;

    const float c = 0.5; // Catmull-Rom
    float2 w0 = -c         * f3 + 2.0 * c         * f2 - c * f;
    float2 w1 =  (2.0 - c) * f3 - (3.0 - c)       * f2         + 1.0;
    float2 w2 = -(2.0 - c) * f3 + (3.0 - 2.0 * c) * f2 + c * f;
    float2 w3 =  c         * f3 - c               * f2;

    float2 w12 = w1 + w2;
    float2 tc0 = texelSize * (tc1 - 1.0);
    float2 tc3 = texelSize * (tc1 + 2.0);
    float2 tc12 = texelSize * (tc1 + w2 / w12);

    float3 s0 = SampleHistoryTap(float2(tc12.x, tc0.y));
    float3 s1 = SampleHistoryTap(float2(tc0.x, tc12.y));
    float3 s2 = SampleHistoryTap(float2(tc12.x, tc12.y));
    float3 s3 = SampleHistoryTap(float2(tc3.x, tc12.y));
    float3 s4 = SampleHistoryTap(float2(tc12.x, tc3.y));

    float cw0 = w12.x * w0.y;
    float cw1 = w0.x * w12.y;
    float cw2 = w12.x * w12.y;
    float cw3 = w3.x * w12.y;
    float cw4 = w12.x * w3.y;

    float3 filtered = s0 * cw0 + s1 * cw1 + s2 * cw2 + s3 * cw3 + s4 * cw4;
    float weightSum = cw0 + cw1 + cw2 + cw3 + cw4;
    return filtered / weightSum;
}

// Clip history toward the color-box center along the line to the box (INSIDE clip_aabb,
// TAA_Guide 3.3(2)). Vs per-channel clamp this stays on the box's convex hull, preserving hue.
float3 ClipHistoryToBox(float3 history, float3 boxMin, float3 boxMax)
{
    float3 center = 0.5 * (boxMax + boxMin);
    float3 extents = max(0.5 * (boxMax - boxMin), HALF_MIN);
    float3 offset = history - center;
    float3 unit = offset / extents;
    float maxUnit = Max3(abs(unit.x), abs(unit.y), abs(unit.z));
    return (maxUnit > 1.0) ? center + offset / maxUnit : history;
}

float4 TemporalAAResolvePassFragment(Varyings_Default input) : SV_TARGET
{
    float2 uv = input.screenUV;

    // Current (jittered) sample and its 3x3 neighborhood, in YCoCg so the color box decouples
    // chroma from luma and clips with less color shift (TAA_Guide 3.4, Karis14).
    float4 sourceCenter = SampleSource(uv);
    float3 current = RGBToYCoCg(sourceCenter.rgb);

    float3 boxMin = current;
    float3 boxMax = current;
    float3 moment1 = current;
    float3 moment2 = current * current;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            float3 neighbor = RGBToYCoCg(SampleSourceRGB(uv, float2(x, y)));
            boxMin = min(boxMin, neighbor);
            boxMax = max(boxMax, neighbor);
            moment1 += neighbor;
            moment2 += neighbor * neighbor;
        }
    }

    // Variance clipping (TAA_Guide 3.3(3), Salvi16): tighten the box to mean +- gamma * stdDev so
    // outliers do not inflate it. Intersect with the min/max box so it is never looser than that.
    const float perSample = 1.0 / 9.0;
    float3 mean = moment1 * perSample;
    float3 stdDev = sqrt(abs(moment2 * perSample - mean * mean));
    float3 devMin = mean - _TAAVarianceClampScale * stdDev;
    float3 devMax = mean + _TAAVarianceClampScale * stdDev;
    boxMin = max(boxMin, devMin);
    boxMax = min(boxMax, devMax);

    // Reproject, resample history (Catmull-Rom), then clip it into the box.
    float2 historyUV = ReprojectToHistoryUV(uv);
    float3 history = RGBToYCoCg(SampleHistoryBicubic(historyUV));
    history = ClipHistoryToBox(history, boxMin, boxMax);

    // Off-screen history is invalid (no data reprojected in): fall back to the current sample.
    float blend = _TAAFrameInfluence;
    if (historyUV.x < 0.0 || historyUV.x > 1.0 || historyUV.y < 0.0 || historyUV.y > 1.0)
    {
        blend = 1.0;
    }

    // Exponential accumulation: out = alpha * current + (1 - alpha) * history (in YCoCg).
    float3 result = lerp(history, current, blend);

    // Guard the history feedback loop: the sharpening cubic and YCoCg round-trip can produce small
    // negatives that would otherwise accumulate. Clamp to non-negative before it becomes history.
    return float4(max(YCoCgToRGB(result), 0.0), sourceCenter.a);
}

#endif
