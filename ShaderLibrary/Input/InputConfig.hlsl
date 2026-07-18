#ifndef ARCTOON_INPUT_CONFIG_INCLUDED
#define ARCTOON_INPUT_CONFIG_INCLUDED

#include "../RegionID.hlsl"

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig
{
    Fragment fragment;
    float2 baseUV;
    int regionIndex;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV)
{
    InputConfig config;
    config.fragment = GetFragment(positionSS);
    config.baseUV = baseUV;
    config.regionIndex = 0;
    return config;
}

// Region ID aware overload. Resolves the region index once at construction
// so downstream code only consumes `config.regionIndex`.
// Requires the caller's CBUFFER to declare _RegionCount and _RegionIDChannel.
InputConfig GetInputConfig(float4 positionSS, float2 baseUV, float4 vertexColor)
{
    InputConfig config = GetInputConfig(positionSS, baseUV);
    #if defined(_REGION_ID_TEXTURE)
    float4 idSample = SAMPLE_TEXTURE2D(_RegionIDMap, sampler_point_clamp, baseUV);
    float raw = SelectChannel(idSample, INPUT_PROP(_RegionIDChannel));
    config.regionIndex = ResolveRegionIndex(raw, INPUT_PROP(_RegionCount));
    #elif defined(_REGION_ID_VERTEX_COLOR)
    float raw = SelectChannel(vertexColor, INPUT_PROP(_RegionIDChannel));
    config.regionIndex = ResolveRegionIndex(raw, INPUT_PROP(_RegionCount));
    #endif
    return config;
}

#endif
