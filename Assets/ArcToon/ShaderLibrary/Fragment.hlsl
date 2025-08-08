#ifndef ARCTOON_FRAGMENT_INCLUDED
#define ARCTOON_FRAGMENT_INCLUDED

#include "Input/UnityInput.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_StencilMaskTexture);

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

Fragment GetFragment(float4 positionSS)
{
    Fragment fragment;
    fragment.positionSS = positionSS.xy;
    fragment.screenUV = fragment.positionSS * _CameraBufferSize.xy;
    fragment.depth = positionSS.z;
    fragment.linearDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    fragment.bufferDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, fragment.screenUV);
    fragment.bufferLinearDepth = IsOrthographicCamera()
                                     ? OrthographicDepthBufferToLinear(fragment.bufferDepth)
                                     : LinearEyeDepth(fragment.bufferDepth, _ZBufferParams);
    float4 stencilMask = SAMPLE_TEXTURE2D(_StencilMaskTexture, sampler_linear_clamp, fragment.screenUV);
    fragment.stencilMask = stencilMask;
    return fragment;
}

#endif
