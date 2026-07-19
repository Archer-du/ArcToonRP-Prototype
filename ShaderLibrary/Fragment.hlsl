#ifndef ARCTOON_FRAGMENT_INCLUDED
#define ARCTOON_FRAGMENT_INCLUDED

#include "Input/UnityInput.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_StencilMaskTexture);

// Render attachment size (post render-scale, not the display resolution).
// .x = 1/width, .y = 1/height, .z = width, .w = height.
float4 _CameraBufferSize;

struct Fragment
{
    float2 positionSS;
    float2 screenUV;
    float depth;
    float linearDepth;
    float bufferDepth;
    float bufferLinearDepth;
    float4 stencilMask;
};

// Per-fragment shared context aggregating the screen-space Fragment, the base UV, and the
// region index used for per-region parameter selection. The region index is resolved by the
// Region ID system (RegionID.hlsl); all consumers read it back as a plain int.
struct InputConfig
{
    Fragment fragment;
    float2 baseUV;
    int regionIndex;
};

bool IsOrthographicCamera()
{
    return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear(float rawDepth)
{
    #if UNITY_REVERSED_Z
    rawDepth = 1.0 - rawDepth;
    #endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

float2 GetScreenUV(float4 positionSS)
{
    return positionSS.xy * _CameraBufferSize.xy;
}

float GetLinearDepth(float4 positionSS)
{
    return IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
}

Fragment GetFragment(float4 positionSS)
{
    Fragment fragment;
    fragment.positionSS = positionSS.xy;
    fragment.screenUV = GetScreenUV(positionSS);
    fragment.depth = positionSS.z;
    fragment.linearDepth = GetLinearDepth(positionSS);
    fragment.bufferDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, fragment.screenUV);
    fragment.bufferLinearDepth = IsOrthographicCamera()
                                     ? OrthographicDepthBufferToLinear(fragment.bufferDepth)
                                     : LinearEyeDepth(fragment.bufferDepth, _ZBufferParams);
    float4 stencilMask = SAMPLE_TEXTURE2D(_StencilMaskTexture, sampler_linear_clamp, fragment.screenUV);
    fragment.stencilMask = stencilMask;
    return fragment;
}

#endif
