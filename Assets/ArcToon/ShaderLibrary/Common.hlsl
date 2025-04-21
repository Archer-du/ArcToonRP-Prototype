#ifndef ARCTOON_COMMON_INCLUDED
#define ARCTOON_COMMON_INCLUDED

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM
#define UNITY_MATRIX_P glstate_matrix_projection

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#include "Input/UnityInput.hlsl"

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

#include "Fragment.hlsl"
#include "ForwardPlus.hlsl"

float Square(float v)
{
    return v * v;
}

float DistanceSquared(float3 pA, float3 pB)
{
    return dot(pA - pB, pA - pB);
}

float FadedStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

float SigmoidSharp(float x, float center, float sharp)
{
    float s = 1.0 / (1.0 + pow(100000.0, (-3.0 * sharp * (x - center))));
    return s;
};

void ClipLOD(Fragment fragment, float fade)
{
    #if defined(LOD_FADE_CROSSFADE)
    float dither = InterleavedGradientNoise(fragment.positionSS.xy, 0);;
    clip((fade < 0 ? fade + 1 : fade) - dither);
    #endif
}

float3 DecodeNormal(float4 sample, float scale = 1.0)
{
    #if defined(UNITY_NO_DXT5nm)
    return normalize(UnpackNormalRGB(sample, scale));
    #else
    return normalize(UnpackNormalmapRGorAG(sample, scale));
    #endif
}

float4 TransformObjectToWorldTangent(float4 tangentOS)
{
    return float4(TransformObjectToWorldDir(tangentOS.xyz), tangentOS.w);
}

float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS, bool doNormalize = false)
{
    float3x3 tangentToWorld =
        CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld, doNormalize);
}

#endif
