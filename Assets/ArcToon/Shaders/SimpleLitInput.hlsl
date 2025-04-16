#ifndef ARCTOON_SIMPLELIT_INPUT_INCLUDED
#define ARCTOON_SIMPLELIT_INPUT_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Input/InputConfig.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_DetailMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_DetailNormalMap);
TEXTURE2D(_MODSMaskMap);
SAMPLER(sampler_DetailMap);
TEXTURE2D(_EmissionMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
    UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float2 TransformBaseUV(float2 rawBaseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return rawBaseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV(float2 rawDetailUV)
{
    float4 detailST = INPUT_PROP(_DetailMap_ST);
    return rawDetailUV * detailST.xy + detailST.zw;
}

float4 GetDetail(InputConfig input)
{
    if (input.useDetail)
    {
        float4 detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, input.detailUV);
        return (detail * 2.0 - 1.0) *
            float4(INPUT_PROP(_DetailAlbedo), 0, INPUT_PROP(_DetailSmoothness), 0);
    }
    return 0.0;
}

float4 GetMODSMask(InputConfig input)
{
    if (input.useMODSMask)
    {
        return SAMPLE_TEXTURE2D(_MODSMaskMap, sampler_BaseMap, input.baseUV);
    }
    return 1.0;
}

float4 GetColor(InputConfig input)
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    if (input.useDetail)
    {
        float detail = GetDetail(input).r;
        float detailMask = GetMODSMask(input).b;
        albedo.rgb = lerp(albedo.rgb, detail < 0.0 ? 0.0 : 1.0, detailMask * abs(detail));
    }
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
    if (input.useDetail)
    {
        packedNormal = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, input.detailUV);
        scale = INPUT_PROP(_DetailNormalScale) * GetMODSMask(input).b;
        float3 detail = DecodeNormal(packedNormal, scale);
        normal = BlendNormalRNM(normal, detail);
    }
    return normal;
}

float GetMetallic(InputConfig input)
{
    float metallic = INPUT_PROP(_Metallic);
    metallic *= GetMODSMask(input).r;
    return metallic;
}

float GetSmoothness(InputConfig input)
{
    float smoothness = INPUT_PROP(_Smoothness);
    smoothness *= GetMODSMask(input).a;

    float detail = GetDetail(input).b;
    float mask = GetMODSMask(input).b;
    smoothness = lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);

    return smoothness;
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

float GetOcclusion(InputConfig input)
{
    float strength = INPUT_PROP(_Occlusion);
    float occlusion = GetMODSMask(input).g;
    occlusion = lerp(1.0, occlusion, strength);
    return occlusion;
}

float GetFinalAlpha(float alpha)
{
    return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}

#endif
