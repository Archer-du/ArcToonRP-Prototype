#ifndef ARCTOON_TOON_CORE_INPUT_INCLUDED
#define ARCTOON_TOON_CORE_INPUT_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Input/InputConfig.hlsl"
#include "../ShaderLibrary/Light/Lighting.hlsl"

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

TEXTURE2D(_TangentShiftMap);
SAMPLER(sampler_TangentShiftMap);

TEXTURE2D(_ParallaxSpecMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)

    UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)

    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)

    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
    UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)

    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)

    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightAttenOffset)
    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightAttenSmooth)
    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightAttenSmoothNew)

    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightSpecOffset)
    UNITY_DEFINE_INSTANCED_PROP(float, _DirectLightSpecSmooth)

    UNITY_DEFINE_INSTANCED_PROP(float4, _OutlineColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _OutlineScale)

    UNITY_DEFINE_INSTANCED_PROP(float, _RimScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _RimWidth)
    UNITY_DEFINE_INSTANCED_PROP(float, _RimDepthBias)

    UNITY_DEFINE_INSTANCED_PROP(float4, _LightMapSDF_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _ShadowOffsetSDF)
    UNITY_DEFINE_INSTANCED_PROP(float4, _FaceVector)

    UNITY_DEFINE_INSTANCED_PROP(float, _NoseSpecularStrengthSDF)
    UNITY_DEFINE_INSTANCED_PROP(float, _NoseSpecularSmoothSDF)

    UNITY_DEFINE_INSTANCED_PROP(float, _SpecGloss)
    UNITY_DEFINE_INSTANCED_PROP(float, _SpecScale)

    UNITY_DEFINE_INSTANCED_PROP(float4, _TangentShiftMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _TangentShiftOffset)

    UNITY_DEFINE_INSTANCED_PROP(float4, _ParallaxSpecMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _ParallaxSensitivity)
    UNITY_DEFINE_INSTANCED_PROP(float, _ParallaxOffset)

    UNITY_DEFINE_INSTANCED_PROP(float, _FringeShadowBiasScaleX)
    UNITY_DEFINE_INSTANCED_PROP(float, _FringeShadowBiasScaleY)
    UNITY_DEFINE_INSTANCED_PROP(float, _FringeTransparentScale)

    UNITY_DEFINE_INSTANCED_PROP(float, _PerObjectShadowCasterID)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROPS_DIRECT_ATTEN_PARAMS \
INPUT_PROP(_DirectLightAttenOffset), \
INPUT_PROP(_DirectLightAttenSmooth), \
INPUT_PROP(_DirectLightAttenSmoothNew)

#define INPUT_PROPS_DIRECT_SPEC_PARAMS \
INPUT_PROP(_DirectLightSpecOffset), \
INPUT_PROP(_DirectLightSpecSmooth)

#define STENCIL_MASK_CHANNEL_FRINGE_SHADOW g
#define STENCIL_MASK_CHANNEL_EYE_LASHES b

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    float2 UV1 : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_ATTRIBUTES_DATA
};

// common ---------------------------------------------------------------------------
float2 TransformBaseUV(float2 rawBaseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return rawBaseUV * baseST.xy + baseST.zw;
}

float2 TransformFaceUV(float2 rawFaceUV)
{
    float4 faceST = INPUT_PROP(_LightMapSDF_ST);
    return rawFaceUV * faceST.xy + faceST.zw;
}

float2 TransformHairUV(float2 rawHairUV)
{
    float4 baseST = INPUT_PROP(_TangentShiftMap_ST);
    return rawHairUV * baseST.xy + baseST.zw;
}

float4 GetColor(InputConfig input)
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    float4 color = INPUT_PROP(_BaseColor);
    return albedo * color;
}

float3 GetNormalTS(InputConfig input)
{
    float4 packedNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, input.baseUV);
    float scale = INPUT_PROP(_NormalScale);
    float3 normal = DecodeNormal(packedNormal, scale);
    return normal;
}

float GetAlphaClip(InputConfig input)
{
    return INPUT_PROP(_Cutoff);
}

float GetPerObjectShadowCasterID()
{
    return INPUT_PROP(_PerObjectShadowCasterID);
}

// PBR ---------------------------------------------------------------------------
float4 GetRMOMask(InputConfig input)
{
    #if defined(_RMO_MASK_MAP)
    return SAMPLE_TEXTURE2D(_RMOMaskMap, sampler_RMOMaskMap, input.baseUV);
    #endif
    return 1.0;
}

float GetMetallic(InputConfig input)
{
    float metallic = INPUT_PROP(_Metallic);
    metallic *= GetRMOMask(input).g;
    return metallic;
}

float GetSmoothness(InputConfig input)
{
    #if defined(_RMO_MASK_MAP)
    float smoothness = PerceptualRoughnessToPerceptualSmoothness(GetRMOMask(input).r);
    smoothness *= INPUT_PROP(_Smoothness);
    #else
    float smoothness = INPUT_PROP(_Smoothness);
    #endif
    return smoothness;
}

float GetOcclusion(InputConfig input)
{
    float strength = INPUT_PROP(_Occlusion);
    float occlusion = GetRMOMask(input).b;
    occlusion = lerp(1.0, occlusion, strength);
    return occlusion;
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

// Toon ---------------------------------------------------------------------------
float GetOutlineScale()
{
    return INPUT_PROP(_OutlineScale) * 12.5;
}

float3 GetOutlineColor()
{
    return INPUT_PROP(_OutlineColor).rgb;
}

float GetRimLightScale()
{
    return INPUT_PROP(_RimScale) * 12.5;
}

float GetRimLightWidth()
{
    return INPUT_PROP(_RimWidth) * 0.07;
}

float GetRimLightDepthBias()
{
    return INPUT_PROP(_RimDepthBias);
}

float GetFringeTransparentScale()
{
    return INPUT_PROP(_FringeTransparentScale);
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

float3 SampleRampSetChannel(float rampUV, float channel)
{
    #if defined(_RAMP_SET)
    return SAMPLE_TEXTURE2D(_RampSet, sampler_RampSet, float2(rampUV, channel)).rgb;
    #endif
    return 1.0;
}

float3 GetFaceVector()
{
    return INPUT_PROP(_FaceVector).xyz;
}

float SampleSDFLightMap(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).r;
    #endif
    return 1.0;
}

float SampleSDFLightMapShadowMask(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).a;
    #endif
    return 1.0;
}

float SampleSDFLightMapNoseSpecular1(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP_SPEC)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).g;
    #endif
    return 0.0;
}

float SampleSDFLightMapNoseSpecular2(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP_SPEC)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).b;
    #endif
    return 0.0;
}

float GetSpecGloss()
{
    return max(0.001, INPUT_PROP(_SpecGloss) * 200);
}

float GetSpecScale()
{
    return INPUT_PROP(_SpecScale) * 50;
}

float GetTangentShiftOffset()
{
    return INPUT_PROP(_TangentShiftOffset);
}

float SampleTangentShiftNoise(float2 baseUV)
{
    return clamp(-0.8, 0.8, SAMPLE_TEXTURE2D(_TangentShiftMap, sampler_TangentShiftMap, baseUV).r * 2.0 - 1.0);
}

float GetParallaxSensitivity()
{
    return INPUT_PROP(_ParallaxSensitivity) * 0.1;
}

float GetParallaxOffset()
{
    return INPUT_PROP(_ParallaxOffset);
}

float SampleParallaxSpecularMask(float2 hairUV)
{
    return SAMPLE_TEXTURE2D(_ParallaxSpecMap, sampler_linear_clamp, hairUV).r;
}

float2 GetFringeShadowBiasScale()
{
    float2 data;
    data.x = INPUT_PROP(_FringeShadowBiasScaleX) * 0.2;
    data.y = INPUT_PROP(_FringeShadowBiasScaleY) * 0.2;
    return data;
}

#endif