#ifndef ARCTOON_TOON_BASE_INPUT_INCLUDED
#define ARCTOON_TOON_BASE_INPUT_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Input/InputConfig.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_RMOMaskMap);
TEXTURE2D(_EmissionMap);

TEXTURE2D(_RampSet);
SAMPLER(sampler_RampSet);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)

    UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
    UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)

    UNITY_DEFINE_INSTANCED_PROP(float4, _OutlineColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _OutlineScale)

    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float2 TransformBaseUV(float2 rawBaseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return rawBaseUV * baseST.xy + baseST.zw;
}

float4 GetRMOMask(InputConfig input)
{
    #ifdef _RMO_MASK_MAP
    return SAMPLE_TEXTURE2D(_RMOMaskMap, sampler_BaseMap, input.baseUV);
    #endif
    return 1.0;
}

float4 GetColor(InputConfig input)
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    float4 color = INPUT_PROP(_BaseColor);
    return albedo * color;
}

float GetAlphaClip(InputConfig input)
{
    return INPUT_PROP(_Cutoff);
}

float3 GetNormalTS(InputConfig input)
{
    float4 packedNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, input.baseUV);
    float scale = INPUT_PROP(_NormalScale);
    float3 normal = DecodeNormal(packedNormal, scale);
    return normal;
}

float GetMetallic(InputConfig input)
{
    float metallic = INPUT_PROP(_Metallic);
    metallic *= GetRMOMask(input).r;
    return metallic;
}

float GetSmoothness(InputConfig input)
{
    float smoothness = INPUT_PROP(_Smoothness);
    smoothness *= GetRMOMask(input).g;
    return smoothness;
}

float GetOcclusion(InputConfig input)
{
    float strength = INPUT_PROP(_Occlusion);
    float occlusion = GetRMOMask(input).b;
    occlusion = lerp(1.0, occlusion, strength);
    return occlusion;
}

float GetOutlineScale()
{
    return INPUT_PROP(_OutlineScale);
}

float GetOutlineColor()
{
    return INPUT_PROP(_OutlineColor);
}

float GetFresnel(InputConfig input)
{
    return INPUT_PROP(_Fresnel);
}

float3 GetEmission(InputConfig input)
{
    float4 albedo = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, input.baseUV);
    float4 color = INPUT_PROP(_EmissionColor);
    return albedo.rgb * color.rgb;
}

float GetFinalAlpha(float alpha)
{
    return 1.0;
}

#endif
