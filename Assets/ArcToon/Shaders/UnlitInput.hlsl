#ifndef ARCTOON_UNLIT_INPUT_INCLUDED
#define ARCTOON_UNLIT_INPUT_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Input/InputConfig.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float2 TransformBaseUV(float2 rawBaseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return rawBaseUV * baseST.xy + baseST.zw;
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

float GetMetallic(InputConfig input)
{
    return 0.0;
}

float GetSmoothness(InputConfig input)
{
    return 0.0;
}

float GetFresnel(InputConfig input)
{
    return 0.0;
}

float3 GetEmission(InputConfig input)
{
    return GetColor(input).rgb;
}

#endif
