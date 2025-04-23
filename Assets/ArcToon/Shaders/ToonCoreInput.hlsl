#ifndef ARCTOON_TOON_CORE_INPUT_INCLUDED
#define ARCTOON_TOON_CORE_INPUT_INCLUDED


#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Input/InputConfig.hlsl"

TEXTURE2D(_BaseMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_RMOMaskMap);
SAMPLER(sampler_RMOMaskMap);

TEXTURE2D(_LightMapSDF);
SAMPLER(sampler_LightMapSDF);

TEXTURE2D(_RampSet);
SAMPLER(sampler_RampSet);

TEXTURE2D(_SpecMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _LightMapSDF_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _SpecMap_ST)

    UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _SpecScale)

    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)

    UNITY_DEFINE_INSTANCED_PROP(float, _ShadowOffsetSDF)

    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
    UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)

    UNITY_DEFINE_INSTANCED_PROP(float4, _OutlineColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _OutlineScale)

    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)

    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightSpecSigmoidCenter)
    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightSpecSigmoidSharp)

    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightAttenOffset)
    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightAttenSmooth)

    UNITY_DEFINE_INSTANCED_PROP(float, _NoseSpecularStrengthSDF)
    UNITY_DEFINE_INSTANCED_PROP(float, _NoseSpecularSmoothSDF)

    UNITY_DEFINE_INSTANCED_PROP(float, _FringeShadowBiasScaleX)
    UNITY_DEFINE_INSTANCED_PROP(float, _FringeShadowBiasScaleY)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROPS_DIRECT_ATTEN_PARAMS \
INPUT_PROP(_DirectLightAttenOffset), \
INPUT_PROP(_DirectLightAttenSmooth)

float2 TransformBaseUV(float2 rawBaseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return rawBaseUV * baseST.xy + baseST.zw;
}

float2 TransformFaceUV(float2 rawFaceUV)
{
    float4 faceST = INPUT_PROP(_LightMapSDF_ST);
    return rawFaceUV * faceST.xy * 0.1 + faceST.zw * 0.1;
}

float4 GetRMOMask(InputConfig input)
{
    #ifdef _RMO_MASK_MAP
    return SAMPLE_TEXTURE2D(_RMOMaskMap, sampler_RMOMaskMap, input.baseUV);
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
    metallic *= GetRMOMask(input).g;
    return metallic;
}

float GetSmoothness(InputConfig input)
{
    float smoothness = PerceptualRoughnessToPerceptualSmoothness(GetRMOMask(input).r);
    smoothness *= INPUT_PROP(_Smoothness);
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

float3 GetOutlineColor()
{
    return INPUT_PROP(_OutlineColor).rgb;
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

float GetSpecular(InputConfig input)
{
    // TODO:
    #ifdef _SPEC_MAP
    float4 baseST = INPUT_PROP(_SpecMap_ST);
    float2 baseUV = input.baseUV * baseST.xy * 0.1 + baseST.zw * 0.1;
    float specularScale = INPUT_PROP(_SpecScale);
    return SAMPLE_TEXTURE2D(_SpecMap, sampler_BaseMap, baseUV).rgb * specularScale;
    #endif
    return 0.0;
}

float GetFinalAlpha(float alpha)
{
    return 1.0;
}

float3 SampleRampSetChannel(float rampUV, float channel)
{
    #ifdef _RAMP_SET
    return SAMPLE_TEXTURE2D(_RampSet, sampler_RampSet, float2(rampUV, channel)).rgb;
    #endif
    return 1.0;
}

float SampleSDFLightMap(float2 faceUV)
{
    #ifdef _SDF_LIGHT_MAP
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).r;
    #endif
    return 1.0;
}

float SampleSDFLightMapShadowMask(float2 faceUV)
{
    #ifdef _SDF_LIGHT_MAP
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).a;
    #endif
    return 1.0;
}

float SampleSDFLightMapNoseSpecular1(float2 faceUV)
{
    #ifdef _SDF_LIGHT_MAP_SPEC
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).g;
    #endif
    return 1.0;
}

float SampleSDFLightMapNoseSpecular2(float2 faceUV)
{
    #ifdef _SDF_LIGHT_MAP_SPEC
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).b;
    #endif
    return 1.0;
}

float GetNoseSpecularStrength()
{
    return INPUT_PROP(_NoseSpecularStrengthSDF) * 50;
}

float GetNoseSpecularSmooth()
{
    return INPUT_PROP(_NoseSpecularSmoothSDF) * 2;
}

float GetSDFShadowOffset()
{
    return INPUT_PROP(_ShadowOffsetSDF) * 0.25;
}

float2 GetFringeShadowBiasScale()
{
    float2 data;
    data.x = INPUT_PROP(_FringeShadowBiasScaleX) * 0.2;
    data.y = INPUT_PROP(_FringeShadowBiasScaleY) * 0.2;
    return data;
}

#endif