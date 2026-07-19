#ifndef ARCTOON_SURFACE_INTERFACE_INCLUDED
#define ARCTOON_SURFACE_INTERFACE_INCLUDED

// Interface tier: base surface / PBR / emission getters. Reads the per-material CBUFFER,
// so it MUST be included after the shader's UnityPerMaterial block.
// Requires: SurfaceSampling.hlsl (INPUT_PROP, _BaseMap, SampleAlbedo), Common.hlsl (Fragment.hlsl provides InputConfig).

TEXTURE2D(_NormalMap);
TEXTURE2D(_EmissionMap);
TEXTURE2D(_MetallicMap);
TEXTURE2D(_RoughnessMap);
TEXTURE2D(_OcclusionMap);

float2 TransformBaseUV(float2 rawBaseUV)
{
    return TransformUVWithST(rawBaseUV, INPUT_PROP(_BaseMap_ST));
}

float4 GetAlbedo(InputConfig input)
{
    return SampleAlbedo(input.baseUV, INPUT_PROP(_BaseColor));
}

float4 GetAlbedo(float2 baseUV)
{
    return SampleAlbedo(baseUV, INPUT_PROP(_BaseColor));
}

float3 GetNormalTS(InputConfig input)
{
    float4 packedNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, input.baseUV);
    float scale = INPUT_PROP(_NormalScale);
    return DecodeNormal(packedNormal, scale);
}

float GetAlphaClip(InputConfig input)
{
    return INPUT_PROP(_Cutoff);
}

float GetPerObjectShadowCasterID()
{
    return INPUT_PROP(_PerObjectShadowCasterID);
}

float GetMetallic(InputConfig input)
{
    float metallic = INPUT_PROP(_Metallic);
    #if defined(_METALLIC_MAP)
    float4 map = SAMPLE_TEXTURE2D(_MetallicMap, sampler_BaseMap, input.baseUV);
    metallic *= SelectChannel(map, INPUT_PROP(_MetallicMapChannel));
    #endif
    return metallic;
}

float GetRoughness(InputConfig input)
{
    float perceptualRoughness = INPUT_PROP(_Roughness);
    #if defined(_ROUGHNESS_MAP)
    float4 map = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_BaseMap, input.baseUV);
    perceptualRoughness *= SelectChannel(map, INPUT_PROP(_RoughnessMapChannel));
    #endif
    return PerceptualRoughnessToRoughness(perceptualRoughness);
}

float GetOcclusion(InputConfig input)
{
    float strength = INPUT_PROP(_Occlusion);
    float occlusion = 1.0;
    #if defined(_OCCLUSION_MAP)
    float4 map = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, input.baseUV);
    occlusion = SelectChannel(map, INPUT_PROP(_OcclusionMapChannel));
    #endif
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

#endif
