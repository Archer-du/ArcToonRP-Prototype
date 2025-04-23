#ifndef ARCTOON_FRAGMENT_INCLUDED
#define ARCTOON_FRAGMENT_INCLUDED

#include "Input/UnityInput.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraColorTexture);
TEXTURE2D(_StencilMaskTexture);

float4 _CameraBufferSize;

struct Fragment
{
    float2 positionSS;
    float2 screenUV;
    float linearDepth;
    float bufferLinearDepth;
    float stencilMask;
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
    fragment.linearDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    float bufferDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, fragment.screenUV, 0);
    float stencilMask = SAMPLE_TEXTURE2D(_StencilMaskTexture, sampler_linear_clamp, fragment.screenUV).g;
    fragment.stencilMask = stencilMask;
    fragment.bufferLinearDepth = IsOrthographicCamera()
                                     ? OrthographicDepthBufferToLinear(bufferDepth)
                                     : LinearEyeDepth(bufferDepth, _ZBufferParams);
    return fragment;
}

#endif
