#ifndef ARCTOON_TEMPORAL_AA_PASS_INCLUDED
#define ARCTOON_TEMPORAL_AA_PASS_INCLUDED

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

TEXTURE2D(_TAAHistoryTexture);

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

float4 TemporalAAResolvePassFragment(Varyings_Default input) : SV_TARGET
{
    float2 uv = input.screenUV;

    // Current (jittered) sample plus its 3x3 neighborhood color box.
    float4 sourceCenter = SampleSource(uv);
    float3 current = sourceCenter.rgb;
    float3 boxMin = current;
    float3 boxMax = current;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            float3 neighbor = SampleSourceRGB(uv, float2(x, y));
            boxMin = min(boxMin, neighbor);
            boxMax = max(boxMax, neighbor);
        }
    }

    // Reproject into the history buffer and fetch the accumulated color (bilinear; Catmull-Rom
    // is a later refinement).
    float2 historyUV = ReprojectToHistoryUV(uv);
    float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistoryTexture, sampler_linear_clamp, historyUV, 0).rgb;

    // Neighborhood clamp keeps stale history within the plausible current color range, suppressing
    // ghosting where history no longer matches the scene.
    history = clamp(history, boxMin, boxMax);

    // Off-screen history is invalid (no data reprojected in): fall back to the current sample.
    float blend = _TAAFrameInfluence;
    if (historyUV.x < 0.0 || historyUV.x > 1.0 || historyUV.y < 0.0 || historyUV.y > 1.0)
    {
        blend = 1.0;
    }

    // Exponential accumulation: out = alpha * current + (1 - alpha) * history.
    float3 result = lerp(history, current, blend);
    return float4(result, sourceCenter.a);
}

#endif
